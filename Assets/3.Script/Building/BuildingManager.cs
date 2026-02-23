using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Flags]
public enum PlacementRule
{
    None = 0,
    GroundOnly = 1,
    NotOnTiltingSurface = 2,
    MustSnap = 4
}

public enum PlacementStatus
{
    Valid,
    Overlap,
    NoSupport,
    NeedGround,
    TooSteep,
    MustSnap
}

[System.Serializable]
public struct BuildingPieceEntry
{
    public string     pieceName;
    public GameObject realPrefab;
    public GameObject ghostPrefab;
    public PlacementRule rules;
}

/// <summary>
/// Phase 5 Building Manager — SmartSnap + Structural Integrity.
/// DontDestroyOnLoad, no PlayerInput dependency.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    // ── UI ────────────────────────────────────────────────────────────────────
    public BuildingUI buildingUI;
    public GameObject buildingUIPanel;  // Bottom bar panel — toggled with build mode
    public UnityEngine.UI.Image[] slotHighlights;
    public bool isBuildMode = false;

    // ── Effects ───────────────────────────────────────────────────────────────
    [Header("Placement Effects")]
    public GameObject placeVFX;
    public AudioClip placeSound;
    [Header("Destruction Effects")]
    public GameObject destroyVFX;
    public AudioClip destroySound;
    private AudioSource _audioSource;
    private GameObject _snapMarker;

    [Header("Debug")]
    public bool showSnapDebug = true;

    // Diagnostic states
    private string _debugSnapStatus = "None"; // "Snapped", "TooFar", "NoCompatible", "FreePlace", "Searching..."
    private int _debugTargetCount = 0;
    private float _debugBestDist = 0f;
    private float _debugBestAlign = 0f;
    private float _debugBestScore = 0f;
    private string _debugPlacementStatus = "Valid"; // "Valid", "SupportFail", "OverlapReject"
    private string _debugOverlapColliderName = "None";

    // ── Pieces ────────────────────────────────────────────────────────────────
    [Header("Available Pieces")]
    public List<BuildingPieceEntry> availablePieces = new List<BuildingPieceEntry>();
    private int selectedIndex = 0;

    // ── Raycast & Snap ────────────────────────────────────────────────────────
    [Header("Raycast & Snap")]
    public LayerMask buildableLayer;
    [SerializeField] private float snapDistance = 1.5f;
    [SerializeField] private float snapRadius   = 1.5f;
    [SerializeField] private LayerMask snapLayer;

    // ── Rotation ──────────────────────────────────────────────────────────────
    private float currentYRotation = 0f;

    // Snap state
    private bool      _isSnapped;
    private Quaternion _snapBaseRot;
    private Transform  _snapTargetSocket;

    // Support state
    private bool _hasSupport;

    // ── Camera Control (Build Mode) ───────────────────────────────────────────
    [Header("Camera Control")]
    [SerializeField] private float rmbDragThreshold = 10f; // Screen pixels
    [SerializeField] private float cameraRotationSpeedMultiplier = 0.5f;

    private Vector2 _rmbDownPosition;
    private bool _isRmbDragging = false;

    private Cinemachine.CinemachineFreeLook _freeLookCam;
    private CameraInputBridge               _camInputBridge;
    private CameraZoom                      _cameraZoom;

    // ── Advanced (BuildingDataSO) ─────────────────────────────────────────────
    [Header("Advanced Placement")]
    private BuildingGhost  currentGhost;
    private BuildingDataSO selectedPiece;
    private LayerMask      buildLayer;
    private Material       ghostMaterialValid;
    private Material       ghostMaterialInvalid;
    private Material       ghostMaterialMustSnap;
    private static MaterialPropertyBlock _mpb;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
    private bool           snapEnabled = true;

    [Header("Building Categories")]
    public List<BuildingCategorySO> categories = new List<BuildingCategorySO>();

    // ── Cached indices ────────────────────────────────────────────────────────
    private int _groundLayer;
    private int _buildingLayer;

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[BuildingManager] Awake -> Instance assigned.");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        buildLayer = LayerMask.GetMask("Ground", "Building", "Terrain");

        // Prepare Audio
        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        // Prepare Snap Marker
        _snapMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _snapMarker.name = "SnapMarker";
        Destroy(_snapMarker.GetComponent<Collider>());
        _snapMarker.transform.localScale = Vector3.one * 0.2f;
        var mr = _snapMarker.GetComponent<Renderer>();
        mr.material.color = Color.yellow; // default yellow
        mr.material.SetFloat("_Glossiness", 0f);
        _snapMarker.transform.SetParent(this.transform);
        _snapMarker.SetActive(false);
        _groundLayer   = LayerMask.NameToLayer("Ground");
        _buildingLayer = LayerMask.NameToLayer("Building");
        if (buildableLayer.value == 0) buildableLayer = buildLayer;

        CreateGhostMaterials();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        _freeLookCam    = Object.FindFirstObjectByType<Cinemachine.CinemachineFreeLook>();
        _camInputBridge = Object.FindFirstObjectByType<CameraInputBridge>();
        _cameraZoom     = Object.FindFirstObjectByType<CameraZoom>();

        foreach (var p in availablePieces)
            if (p.ghostPrefab != null) p.ghostPrefab.SetActive(false);

        Debug.Log("[BuildingManager] Phase 5 Initialized (SmartSnap + Support).");

        // Ensure building UI panel is hidden at startup
        if (buildingUIPanel != null) buildingUIPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Update
    // ═══════════════════════════════════════════════════════════════════════════

    void Update()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        if (kb.bKey.wasPressedThisFrame) ToggleBuildMode();
        if (!isBuildMode) return;

        if (kb.digit1Key.wasPressedThisFrame) SelectPiece(0);
        if (kb.digit2Key.wasPressedThisFrame) SelectPiece(1);
        if (kb.digit3Key.wasPressedThisFrame) SelectPiece(2);
        if (kb.digit4Key.wasPressedThisFrame) SelectPiece(3);

        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f)       { currentYRotation += 45f; _isSnapped = false; }
            else if (scroll < -0.1f) { currentYRotation -= 45f; _isSnapped = false; }
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (currentGhost != null) CancelBuilding();
            else ToggleBuildMode();
            return;
        }

        if (isBuildMode) HandleCameraRotation(mouse);

        var cam = Camera.main;
        if (cam != null && isBuildMode)
        {
            HandleSimpleGhostPlacement(mouse);
            HandleDeconstruction();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Ghost Placement
    // ═══════════════════════════════════════════════════════════════════════════

    private void HandleSimpleGhostPlacement(Mouse mouse)
    {
        if (mouse == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        var ghostGO = GetCurrentGhostPrefab();
        if (ghostGO == null) return;

        int mask = buildableLayer.value != 0 ? buildableLayer.value : buildLayer.value;
        Ray ray  = cam.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, mask))
        {
            if (!ghostGO.activeSelf) ghostGO.SetActive(true);

            // Phase 4.12: 2-Pass Snap Architecture + Rules Pipeline
            BasePlacementPass(hit, ghostGO);
            AutoSnapPass(ghostGO);
            ValidateAndPlacePass(mouse, ghostGO, hit);
        }
        else
        {
            if (ghostGO.activeSelf) ghostGO.SetActive(false);
            _isSnapped  = false;
            _hasSupport = false;
            _snapMarker.SetActive(false);
        }
    }

    private void BasePlacementPass(RaycastHit hit, GameObject ghostGO)
    {
        // Smoothly follow the hit point
        ghostGO.transform.position = Vector3.Lerp(
            ghostGO.transform.position, hit.point, Time.deltaTime * 30f);
        
        // Base rotation from user input
        ghostGO.transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
        
        _isSnapped = false;
    }

    private void AutoSnapPass(GameObject ghostGO)
    {
        // Reset debug vars for this frame
        _debugSnapStatus = "Searching...";
        _debugTargetCount = 0;
        _debugBestDist = 0f;
        _debugBestAlign = 0f;
        _debugBestScore = 0f;

        // 1. Bypass check (Alt Key)
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
        {
            _debugSnapStatus = "FreePlace (Alt)";
            return; // Free placement mode
        }

        // 2. Volumetric search for nearby buildings
        Collider[] hitColliders = Physics.OverlapSphere(ghostGO.transform.position, 0.6f, 1 << _buildingLayer);
        if (hitColliders.Length == 0) 
        {
            _debugSnapStatus = "No Buildings Nearby";
            return;
        }

        // Collect target sockets
        List<SnapPoint> targetSockets = new List<SnapPoint>();
        foreach (var col in hitColliders)
        {
            targetSockets.AddRange(col.GetComponentsInChildren<SnapPoint>());
        }

        _debugTargetCount = targetSockets.Count;

        if (targetSockets.Count == 0) 
        {
            _debugSnapStatus = "No Sockets Found";
            return;
        }

        // 3. Find compatible ghost sockets
        SnapPoint[] ghostSockets = ghostGO.GetComponentsInChildren<SnapPoint>();
        if (ghostSockets.Length == 0) 
        {
            _debugSnapStatus = "Ghost Missing Sockets";
            return;
        }

        SnapPoint bestTarget = null;
        SnapPoint bestGhost  = null;
        float bestScore = float.MaxValue;
        float bestDist  = float.MaxValue;
        float bestAlignForDebug = 0f;

        foreach (var target in targetSockets)
        {
            if (target.isOccupied) continue;

            foreach (var ghost in ghostSockets)
            {
                if (!ghost.CanConnectTo(target)) continue;

                Vector3 ghostSocketWorldPos = ghostGO.transform.TransformPoint(ghost.transform.localPosition);
                Vector3 ghostSocketWorldFwd = ghostGO.transform.TransformDirection(ghost.transform.forward);
                
                float dist  = Vector3.Distance(target.transform.position, ghostSocketWorldPos);
                float align = Vector3.Dot(target.transform.forward, ghostSocketWorldFwd);
                float score = dist + (1f - align) * 0.3f; // instruction formula

                if (score < bestScore)
                {
                    bestScore  = score;
                    bestDist   = dist;
                    bestAlignForDebug = align;
                    bestTarget = target;
                    bestGhost  = ghost;
                }
            }
        }

        // Sync debug variables with the best found
        _debugBestScore = bestScore;
        _debugBestDist  = bestDist;
        _debugBestAlign = bestAlignForDebug;

        if (bestTarget == null || bestGhost == null)
        {
            _debugSnapStatus = "NoCompatible";
            return;
        }

        // Apply Snap if within threshold
        if (bestDist <= 0.5f)
        {
            // Compute rotation
            Vector3 targetFwd = -bestTarget.transform.forward;
            Vector3 targetUp  =  bestTarget.transform.up;
            Quaternion snapRot;
            
            if (targetFwd.sqrMagnitude > 0.01f)
                snapRot = Quaternion.LookRotation(targetFwd, targetUp);
            else
                snapRot = Quaternion.identity;

            Quaternion finalRot = snapRot * Quaternion.Inverse(bestGhost.transform.localRotation);

            if (!Mathf.Approximately(currentYRotation, 0f))
                finalRot = Quaternion.AngleAxis(currentYRotation, bestTarget.transform.up) * finalRot;

            // Compute position
            Transform targetSocket = bestTarget.transform;
            Transform ghostSocket  = bestGhost.transform;

            Vector3 ghostSocketWorldOffset = finalRot * ghostSocket.localPosition;
            Vector3 snappedPos = targetSocket.position - ghostSocketWorldOffset;

            // Apply to ghost
            ghostGO.transform.position = snappedPos;
            ghostGO.transform.rotation = finalRot;

            // Cache state
            _isSnapped        = true;
            _snapBaseRot      = finalRot;
            _snapTargetSocket = targetSocket;

            _debugSnapStatus = "Snapped!";
        }
        else
        {
            _debugSnapStatus = "TooFar (>0.5)";
        }
    }



    // ═══════════════════════════════════════════════════════════════════════════
    // Camera Control & Deconstruction
    // ═══════════════════════════════════════════════════════════════════════════

    private void HandleCameraRotation(Mouse mouse)
    {
        if (_freeLookCam == null || mouse == null) return;

        // On press, record position
        if (mouse.rightButton.wasPressedThisFrame)
        {
            _rmbDownPosition = mouse.position.ReadValue();
            _isRmbDragging = false;
        }

        // While holding, check drag threshold and apply rotation
        if (mouse.rightButton.isPressed)
        {
            Vector2 currentPos = mouse.position.ReadValue();
            if (Vector2.Distance(_rmbDownPosition, currentPos) > rmbDragThreshold)
            {
                _isRmbDragging = true;
            }

            if (_isRmbDragging)
            {
                Vector2 delta = mouse.delta.ReadValue();
                
                // Apply delta to Cinemachine axes. Inverse X for natural feel if needed
                _freeLookCam.m_XAxis.Value += delta.x * cameraRotationSpeedMultiplier;
                _freeLookCam.m_YAxis.Value -= delta.y * (cameraRotationSpeedMultiplier * 0.01f);
                _freeLookCam.m_YAxis.Value = Mathf.Clamp01(_freeLookCam.m_YAxis.Value);
            }
        }
        
        // Also ensure cursor stays confined during build mode so it doesn't leave the screen easily
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void HandleDeconstruction()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        // Right-click to destroy (trigger ONLY on release, and ONLY if we didn't drag to rotate)
        if (mouse.rightButton.wasReleasedThisFrame && !_isRmbDragging)
        {
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            // Only hit objects explicitly on the Building layer
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, 1 << _buildingLayer))
            {
                // Find root object in case we hit a child snap point or sub-collider
                GameObject targetObj = hit.collider.gameObject;
                
                // Usually custom buildings have their main component or are the top-level parent
                // For safety, let's grab the topmost parent that is still on the building layer
                Transform rootTransform = targetObj.transform;
                while (rootTransform.parent != null && rootTransform.parent.gameObject.layer == _buildingLayer)
                {
                    rootTransform = rootTransform.parent;
                }

                // Play Feedback
                if (destroyVFX) Instantiate(destroyVFX, rootTransform.position, rootTransform.rotation);
                if (destroySound && _audioSource) _audioSource.PlayOneShot(destroySound);

                Debug.Log($"[BuildingManager] Destroyed {rootTransform.name}");
                Destroy(rootTransform.gameObject);
                
                // Immediately force overlap check next frame by hiding marker/support status
                _isSnapped = false;
                _hasSupport = false;
                if (_snapMarker) _snapMarker.SetActive(false);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Placement Validation & Rules Pipeline
    // ═══════════════════════════════════════════════════════════════════════════

    private PlacementStatus ValidatePlacement(GameObject ghostGO, RaycastHit hit)
    {
        // 1. Check Overlap using existing logic
        if (CheckOverlap(ghostGO)) return PlacementStatus.Overlap;

        // 2. Get Rules for current piece (safeguard index)
        if (selectedIndex < 0 || selectedIndex >= availablePieces.Count) 
            return PlacementStatus.NoSupport;

        PlacementRule rules = availablePieces[selectedIndex].rules;

        // 3. Rule: GroundOnly
        if ((rules & PlacementRule.GroundOnly) != 0)
        {
            // If not snapped AND hit was not on ground/terrain layer -> Fail
            if (!_isSnapped && hit.collider.gameObject.layer != _groundLayer && hit.collider.gameObject.layer != LayerMask.NameToLayer("Terrain"))
            {
                return PlacementStatus.NeedGround;
            }
        }

        // 4. Rule: NotOnTiltingSurface
        if ((rules & PlacementRule.NotOnTiltingSurface) != 0)
        {
            // hit.normal.y is cos(theta). 0.8f is approx 36 degrees max slope.
            if (!_isSnapped && hit.normal.y < 0.8f)
            {
                return PlacementStatus.TooSteep;
            }
        }

        // 5. Rule: MustSnap
        if ((rules & PlacementRule.MustSnap) != 0)
        {
            // If it MUST snap but currently isn't snapped AND isn't solidly on the ground
            if (!_isSnapped && hit.collider.gameObject.layer != _groundLayer && hit.collider.gameObject.layer != LayerMask.NameToLayer("Terrain"))
            {
                return PlacementStatus.MustSnap;
            }
        }

        // 6. Generic Support check using existing logic
        if (!CheckSupport(ghostGO)) return PlacementStatus.NoSupport;

        return PlacementStatus.Valid;
    }

    private void ValidateAndPlacePass(Mouse mouse, GameObject ghostGO, RaycastHit hit)
    {
        // --- 0. Update Snap Marker visibility ---
        if (_isSnapped && _snapTargetSocket != null)
        {
            _snapMarker.SetActive(true);
            _snapMarker.transform.position = _snapTargetSocket.position;
        }
        else
        {
            _snapMarker.SetActive(false);
        }

        // --- 1. Validation ---
        PlacementStatus status = ValidatePlacement(ghostGO, hit);
        bool isValid = (status == PlacementStatus.Valid);

        _hasSupport = isValid; // For legacy logic compatibility

        // --- 2. Update Debug State ---
        _debugPlacementStatus = status.ToString();
        if (status != PlacementStatus.Overlap) _debugOverlapColliderName = "None"; // Clear if not overlap

        // --- 3. Visual Feedback ---
        ApplyGhostSupportFeedback(ghostGO, isValid); // Turns red if invalid, green if valid

        // --- 4. Placement ---
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // If hovering over UI (e.g. inventory or debug panels), block placement
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (isValid)
            {
                var real = GetCurrentRealPrefab();
                if (real != null)
                {
                    var newObj = Instantiate(real, ghostGO.transform.position, ghostGO.transform.rotation);
                    Debug.Log($"[BuildingManager] Placed {real.name} @ {ghostGO.transform.position}");

                    if (placeVFX) Instantiate(placeVFX, ghostGO.transform.position, ghostGO.transform.rotation);
                    if (placeSound && _audioSource) _audioSource.PlayOneShot(placeSound);
                    
                    // VFX: Pop-up scale & Camera Shake
                    TriggerCameraShake();
                    StartCoroutine(AnimatePlacement(newObj));
                }
            }
            else
            {
                Debug.LogWarning($"[BuildingManager] Cannot place. Reason: {status}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Structural Integrity — CheckSupport
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if the ghost position touches Ground or another Building.
    /// Uses Physics.OverlapBox slightly larger than the ghost's collider bounds.
    /// </summary>
    private bool CheckSupport(GameObject ghostGO)
    {
        // If snapped to a building socket, it's automatically supported
        if (_isSnapped) return true;

        Renderer rend = ghostGO.GetComponentInChildren<Renderer>();
        if (rend == null) return true; // fallback: allow if no renderer

        Bounds bounds   = rend.bounds;
        Vector3 center  = bounds.center;
        Vector3 halfExt = bounds.extents + Vector3.one * 0.15f; // slightly larger

        int supportMask = LayerMask.GetMask("Ground", "Building", "Terrain", "Default");
        Collider[] hits = Physics.OverlapBox(center, halfExt, ghostGO.transform.rotation, supportMask);

        foreach (var col in hits)
        {
            // Skip the ghost itself (Ignore Raycast layer)
            if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
            // Skip same ghost children
            if (col.transform.IsChildOf(ghostGO.transform)) continue;

            int layer = col.gameObject.layer;
            if (layer == _groundLayer || layer == _buildingLayer ||
                layer == LayerMask.NameToLayer("Terrain") ||
                layer == LayerMask.NameToLayer("Default"))
            {
                return true; // Touching ground or building → supported
            }
        }
        return false;
    }

    /// <summary>
    /// Sets the ghost renderer material and color dynamically based on validity and rules.
    /// </summary>
    private void ApplyGhostSupportFeedback(GameObject ghostGO, bool valid)
    {
        // Legacy fallback - not used in 2-Pass architecture, but kept for compatibility
        ApplyGhostSupportFeedback(ghostGO, valid ? PlacementStatus.Valid : PlacementStatus.NoSupport);
    }

    private void ApplyGhostSupportFeedback(GameObject ghostGO, PlacementStatus status)
    {
        Renderer rend = ghostGO.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Material targetMat;
        Color? overrideColor = null;

        switch (status)
        {
            case PlacementStatus.Valid:
                targetMat = ghostMaterialValid;
                // Height-based Green -> Yellow Lerp
                float heightFactor = Mathf.Clamp01(ghostGO.transform.position.y / 15f); // 0 at ground, 1 at 15m high
                Color lerpedColor = Color.Lerp(Color.green, Color.yellow, heightFactor);
                lerpedColor.a = 0.5f; // Keep transparency
                overrideColor = lerpedColor;
                break;

            case PlacementStatus.MustSnap:
                targetMat = ghostMaterialMustSnap; // Blue
                break;

            case PlacementStatus.NoSupport:
            case PlacementStatus.Overlap:
            case PlacementStatus.NeedGround:
            case PlacementStatus.TooSteep:
            default:
                targetMat = ghostMaterialInvalid; // Red
                break;
        }

        if (rend.sharedMaterial != targetMat)
            rend.sharedMaterial = targetMat;

        if (overrideColor.HasValue)
        {
            rend.GetPropertyBlock(_mpb);
            // Fallback to _Color if _BaseColor doesn't exist on standard shader
            if (targetMat.HasProperty("_BaseColor"))
                _mpb.SetColor(ColorProp, overrideColor.Value);
            else if (targetMat.HasProperty("_Color"))
                _mpb.SetColor("_Color", overrideColor.Value);
            rend.SetPropertyBlock(_mpb);
        }
        else
        {
            rend.SetPropertyBlock(null); // Clear overrides
        }
    }

    /// <summary>
    /// Returns true if the ghost overlaps another Building collider (penetration).
    /// Uses a slightly SHRUNK bounding box to allow edge-touching without triggering.
    /// </summary>
    private bool CheckOverlap(GameObject ghostGO)
    {
        _debugOverlapColliderName = "None"; // Reset

        Renderer rend = ghostGO.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        Bounds  bounds  = rend.bounds;
        Vector3 center  = bounds.center;
        // Shrink by 0.05 on each axis so edge-touching doesn't count as overlap
        Vector3 halfExt = bounds.extents - Vector3.one * 0.05f;
        if (halfExt.x < 0.01f) halfExt.x = 0.01f;
        if (halfExt.y < 0.01f) halfExt.y = 0.01f;
        if (halfExt.z < 0.01f) halfExt.z = 0.01f;

        int buildMask = LayerMask.GetMask("Building");
        Collider[] hits = Physics.OverlapBox(center, halfExt, ghostGO.transform.rotation, buildMask);

        foreach (var col in hits)
        {
            if (col.transform.IsChildOf(ghostGO.transform)) continue;
            if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
            
            _debugOverlapColliderName = col.name; // Record the name for debug
            return true; // Overlapping another building
        }
        return false;
    }



    // ═══════════════════════════════════════════════════════════════════════════
    // Debug GUI
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        if (!showSnapDebug || !isBuildMode) return;

        Rect rect = new Rect(10, Screen.height / 2 - 100, 300, 160);
        GUI.Box(rect, "Snap Parameter Debugger");

        GUILayout.BeginArea(new Rect(20, Screen.height / 2 - 80, 280, 140));
        GUILayout.Label($"Target Sockets: {_debugTargetCount}");
        GUILayout.Label($"Snap Status: {_debugSnapStatus}");
        GUILayout.Label($"Best Dist: {_debugBestDist:F3} / Limit: 0.5");
        GUILayout.Label($"Best Align: {_debugBestAlign:F3}");
        GUILayout.Label($"Best Score: {_debugBestScore:F3}");
        
        GUI.color = _debugPlacementStatus == "Valid" ? Color.green : Color.red;
        GUILayout.Label($"Place Status: {_debugPlacementStatus}");
        if (_debugPlacementStatus == "OverlapReject")
        {
            GUILayout.Label($"Overlap with: {_debugOverlapColliderName}");
        }
        GUI.color = Color.white;
        
        GUILayout.EndArea();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Smart Snap — Position + Rotation alignment (Legacy/Advanced)
    // ═══════════════════════════════════════════════════════════════════════════

    private (Vector3 pos, Quaternion rot, bool snapped) TrySnapToSocket(
        RaycastHit hit, GameObject ghostGO)
    {
        // --- 1. Collect target sockets ----------------------------------------
        SnapPoint[] targetSockets = hit.collider.GetComponentsInChildren<SnapPoint>();
        if (targetSockets == null || targetSockets.Length == 0)
            return (hit.point, Quaternion.identity, false);

        // --- 2. Find closest unoccupied target socket -------------------------
        SnapPoint bestTarget = null;
        float minDist = float.MaxValue;
        foreach (var sp in targetSockets)
        {
            if (sp.isOccupied) continue;
            float d = Vector3.Distance(hit.point, sp.transform.position);
            if (d < minDist) { minDist = d; bestTarget = sp; }
        }
        if (bestTarget == null || minDist > snapDistance)
            return (hit.point, Quaternion.identity, false);

        // --- 3. Find compatible ghost socket (distance-based) -----------------
        SnapPoint[] ghostSockets = ghostGO.GetComponentsInChildren<SnapPoint>();
        SnapPoint bestGhostSocket = null;
        float bestGhostDist = float.MaxValue;

        foreach (var gsp in ghostSockets)
        {
            if (!gsp.CanConnectTo(bestTarget)) continue;
            // Pick the ghost socket closest to the target socket
            float d = Vector3.Distance(
                ghostGO.transform.TransformPoint(gsp.transform.localPosition),
                bestTarget.transform.position);
            if (d < bestGhostDist) { bestGhostDist = d; bestGhostSocket = gsp; }
        }

        // If no compatible ghost socket, DON'T snap (prevents pivot-center overlap)
        if (bestGhostSocket == null)
            return (hit.point, Quaternion.identity, false);

        // --- 4. Compute rotation: ghost socket faces -target socket forward ---
        Vector3    targetFwd = -bestTarget.transform.forward;
        Vector3    targetUp  =  bestTarget.transform.up;
        Quaternion snapRot   =  Quaternion.LookRotation(targetFwd, targetUp);

        // Compensate for ghost socket's own local rotation
        Quaternion finalRot = snapRot * Quaternion.Inverse(bestGhostSocket.transform.localRotation);

        // Apply user wheel rotation around target socket's up axis
        if (!Mathf.Approximately(currentYRotation, 0f))
            finalRot = Quaternion.AngleAxis(currentYRotation, bestTarget.transform.up) * finalRot;

        // --- 5. Compute position: socket-to-socket offset ---------------------
        Transform targetSocket = bestTarget.transform;
        Transform ghostSocket  = bestGhostSocket.transform;

        Vector3 ghostSocketWorldOffset = finalRot * ghostSocket.localPosition;
        Vector3 snappedPos = targetSocket.position - ghostSocketWorldOffset;

        // Cache state
        _isSnapped        = true;
        _snapBaseRot      = finalRot;
        _snapTargetSocket = targetSocket;

        Debug.Log($"[BuildingManager] SmartSnap: {bestTarget.socketId} <- {bestGhostSocket.socketId}  " +
                  $"offset={ghostSocketWorldOffset}");
        return (snappedPos, finalRot, true);
    }



    // ═══════════════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════════════

    public void SelectPiece(int index)
    {
        if (availablePieces == null || availablePieces.Count == 0) return;
        index = Mathf.Clamp(index, 0, availablePieces.Count - 1);

        var old = GetCurrentGhostPrefab();
        if (old != null) old.SetActive(false);

        selectedIndex = index;
        currentYRotation = 0f;
        _isSnapped = false;

        // UI Highlight
        if (slotHighlights != null)
        {
            Color normal = new Color(0.18f, 0.15f, 0.12f, 0.95f);
            Color active = new Color(0.45f, 0.65f, 0.25f, 1f);
            for (int i = 0; i < slotHighlights.Length; i++)
            {
                if (slotHighlights[i])
                    slotHighlights[i].color = (i == index) ? active : normal;
            }
        }

        if (isBuildMode)
        {
            var next = GetCurrentGhostPrefab();
            if (next != null)
            {
                next.SetActive(true);
                next.transform.rotation = Quaternion.identity;
            }
        }
        Debug.Log($"[BuildingManager] Piece [{index}]: {availablePieces[index].pieceName}");
    }

    public void ToggleBuildMode()
    {
        isBuildMode = !isBuildMode;
        if (buildingUI) buildingUI.ToggleUI(isBuildMode);

        if (isBuildMode)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.Confined;
            if (_camInputBridge) _camInputBridge.enabled = false;
            if (_cameraZoom)     _cameraZoom.enabled     = false;
            var g = GetCurrentGhostPrefab();
            if (g) g.SetActive(true);
            if (buildingUIPanel != null) buildingUIPanel.SetActive(true);
        }
        else
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (_camInputBridge) _camInputBridge.enabled = true;
            if (_cameraZoom)     _cameraZoom.enabled     = true;
            foreach (var p in availablePieces)
                if (p.ghostPrefab) p.ghostPrefab.SetActive(false);
            _isSnapped = false;
            if (buildingUIPanel != null) buildingUIPanel.SetActive(false);
            if (_snapMarker != null) _snapMarker.SetActive(false);
        }
        Debug.Log($"[BuildingManager] Build Mode: {isBuildMode}");
    }

    public void SelectPiece(BuildingDataSO piece) => StartPlacing(piece);

    public void AddResource(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;
        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(item, amount))
        { Debug.Log($"[BuildingManager] 자원 환급: {item.itemName} x{amount}"); return; }
        if (item.itemPrefab != null)
            for (int i = 0; i < amount; i++)
                Instantiate(item.itemPrefab,
                    transform.position + Vector3.up +
                    UnityEngine.Random.insideUnitSphere * 0.5f,
                    Quaternion.identity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject GetCurrentGhostPrefab() =>
        (availablePieces != null && availablePieces.Count > 0)
            ? availablePieces[selectedIndex].ghostPrefab : null;

    private GameObject GetCurrentRealPrefab() =>
        (availablePieces != null && availablePieces.Count > 0)
            ? availablePieces[selectedIndex].realPrefab : null;

    private void ApplyRotationToCurrentGhost()
    {
        Quaternion rot;
        if (_isSnapped && _snapTargetSocket != null)
            rot = Quaternion.AngleAxis(currentYRotation, _snapTargetSocket.up) * Quaternion.identity;
        else
            rot = Quaternion.Euler(0, currentYRotation, 0);

        var g = GetCurrentGhostPrefab();
        if (g != null) g.transform.rotation = rot;
        if (currentGhost != null) currentGhost.transform.rotation = rot;
    }

    private void StartPlacing(BuildingDataSO data)
    {
        selectedPiece = data; isBuildMode = true;
        if (buildingUI) buildingUI.ToggleUI(false);
        if (currentGhost != null) Destroy(currentGhost.gameObject);
        var go = Instantiate(data.prefab);
        currentGhost = go.AddComponent<BuildingGhost>();
        currentGhost.Setup(ghostMaterialValid, ghostMaterialInvalid);
        currentYRotation = 0f;
        go.transform.rotation = Quaternion.identity;
    }

    private void PlaceObject()
    {
        if (currentGhost == null || selectedPiece == null) return;
        if (currentGhost.isColliding)
        { BuildingFeedback.Instance?.PlayErrorSound(currentGhost.transform.position); return; }
        if (!CheckCost(selectedPiece))
        { BuildingFeedback.Instance?.PlayErrorSound(currentGhost.transform.position); return; }

        var pos = currentGhost.transform.position;
        var rot = currentGhost.transform.rotation;
        var nb  = Instantiate(selectedPiece.prefab, pos, rot);
        var bp  = nb.AddComponent<BuildingPiece>();
        bp.data = selectedPiece;
        bp.CheckGroundedStatus();
        bp.PropagateStabilityUpdate(new HashSet<int>());
        ConsumeCost(selectedPiece);
        BuildingFeedback.Instance?.PlayPlaceSound(pos);
        BuildingFeedback.Instance?.SpawnPlaceVFX(pos);
        
        TriggerCameraShake();
        StartCoroutine(AnimatePlacement(nb));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Placement VFX & Animations
    // ═══════════════════════════════════════════════════════════════════════════

    private System.Collections.IEnumerator AnimatePlacement(GameObject obj)
    {
        if (obj == null) yield break;
        
        Vector3 targetScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;

        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out elastic or simple ease out quad
            t = 1f - (1f - t) * (1f - t); 
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localScale = targetScale;
        }
    }

    private void TriggerCameraShake()
    {
        var impulseSource = Camera.main?.GetComponent<Cinemachine.CinemachineImpulseSource>();
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }

    private void CancelBuilding()
    {
        if (currentGhost != null) { Destroy(currentGhost.gameObject); currentGhost = null; }
        selectedPiece = null; isBuildMode = false; _isSnapped = false;
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        foreach (var p in availablePieces) if (p.ghostPrefab) p.ghostPrefab.SetActive(false);
        Debug.Log("[BuildingManager] Cancelled");
    }

    private bool CheckCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null || data.constructionCosts.Count == 0) return true;
        if (InventorySystem.Instance == null) return false;
        foreach (var c in data.constructionCosts)
            if (c.item != null && !InventorySystem.Instance.HasItem(c.item, c.amount)) return false;
        return true;
    }

    private void ConsumeCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null) return;
        foreach (var c in data.constructionCosts)
            if (c.item != null) InventorySystem.Instance.TryConsume(c.item, c.amount);
    }

    private void CreateGhostMaterials()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        ghostMaterialValid    = new Material(s) { color = new Color(0f, 1f, 0f, 0.5f) };
        ghostMaterialInvalid  = new Material(s) { color = new Color(1f, 0f, 0f, 0.5f) };
        ghostMaterialMustSnap = new Material(s) { color = new Color(0f, 0f, 1f, 0.5f) }; // Blue for MustSnap
        
        SetMaterialTransparent(ghostMaterialValid);
        SetMaterialTransparent(ghostMaterialInvalid);
        SetMaterialTransparent(ghostMaterialMustSnap);
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
        if (mat.HasProperty("_Mode"))    mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private SnapPoint FindClosestSnapPointInScene(Vector3 pos)
    {
        var all = Object.FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
        SnapPoint best = null; float minD = snapRadius;
        foreach (var s in all)
        {
            if (s.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
            float d = Vector3.Distance(pos, s.transform.position);
            if (d < minD) { minD = d; best = s; }
        }
        return best;
    }
}
