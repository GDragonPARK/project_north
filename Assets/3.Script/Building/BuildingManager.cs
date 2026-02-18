using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    public BuildingUI buildingUI;
    public bool isBuildMode = false;

    [Header("Placement System")]
    private BuildingGhost currentGhost;
    private BuildingDataSO selectedPiece;
    private LayerMask buildLayer;
    private Material ghostMaterialValid;
    private Material ghostMaterialInvalid;
    private float rotationAngle = 0f;

    [Header("Snap System")]
    [SerializeField] private float snapRadius = 1.5f;
    [SerializeField] private LayerMask snapLayer; // Layer for placed buildings (to detect their snap points)
    private bool snapEnabled = true; // Toggle with Shift key

    [Header("Building Categories")]
    public List<BuildingCategorySO> categories = new List<BuildingCategorySO>();



    [Header("Input System")]
    private PlayerInput playerInput;
    private InputAction toggleBuildAction;
    private InputAction removeAction;
    private InputAction placeAction;
    private InputAction cancelAction;
    private InputAction rotateAction;
    private InputAction snapToggleAction;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize build layer
        buildLayer = LayerMask.GetMask("Terrain", "Default");

        // Create ghost materials at runtime
        CreateGhostMaterials();

        // Setup Input System
        SetupInputActions();
    }

    void OnEnable()
    {
        if (toggleBuildAction != null) toggleBuildAction.Enable();
        if (removeAction != null) removeAction.Enable();
        if (placeAction != null) placeAction.Enable();
        if (cancelAction != null) cancelAction.Enable();
        if (rotateAction != null) rotateAction.Enable();
        if (snapToggleAction != null) snapToggleAction.Enable();
    }

    void OnDisable()
    {
        if (toggleBuildAction != null) toggleBuildAction.Disable();
        if (removeAction != null) removeAction.Disable();
        if (placeAction != null) placeAction.Disable();
        if (cancelAction != null) cancelAction.Disable();
        if (rotateAction != null) rotateAction.Disable();
        if (snapToggleAction != null) snapToggleAction.Disable();
    }

    private void SetupInputActions()
    {
        playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[BuildingManager] ❌ PlayerInput not found in scene! B key will NOT work.");
            return;
        }

        Debug.Log($"[BuildingManager] ✅ PlayerInput found on '{playerInput.gameObject.name}'");
        Debug.Log($"[BuildingManager] Current ActionMap: {playerInput.currentActionMap?.name ?? "NULL"}");
        Debug.Log($"[BuildingManager] Default Map: {playerInput.defaultActionMap}");

        // Get actions from Input System
        toggleBuildAction = playerInput.actions["ToggleBuildMenu"];
        removeAction = playerInput.actions["Remove"];
        placeAction = playerInput.actions["Attack"]; // Use Attack button for placement
        cancelAction = playerInput.actions["Jump"]; // Use Jump or dedicated for Cancel? Let's try to find or add. 
        // For now, let's assume we use dedicated names if they exist, or fallbacks.
        // I will add these to the .inputactions later.
        rotateAction = playerInput.actions["Look"]; // We'll use scroll logic or R
        snapToggleAction = playerInput.actions["Sprint"];

        // Subscribe to events
        if (toggleBuildAction != null)
        {
            toggleBuildAction.performed += OnToggleBuildMenu;
        }

        if (removeAction != null)
        {
            removeAction.performed += OnRemoveBuilding;
        }

        if (placeAction != null)
        {
            placeAction.performed += OnPlaceBuilding;
        }

        // We'll use Update for things like scroll and continuous checks to avoid callback spam
        // but no more legacy Input calls.
    }

    private void OnToggleBuildMenu(InputAction.CallbackContext context)
    {
        Debug.Log("[BuildingManager] B Key Pressed Action Triggered!");
        ToggleBuildMode();
    }

    private void OnRemoveBuilding(InputAction.CallbackContext context)
    {
        // Only remove in build mode AND when NOT placing a ghost
        if (!isBuildMode || currentGhost != null) return;

        // Raycast to find building
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            BuildingPiece piece = hit.collider.GetComponent<BuildingPiece>();
            if (piece != null)
            {
                piece.RefundResources();
                Debug.Log($"[BuildingManager] Removed building: {hit.collider.gameObject.name}");
                Destroy(hit.collider.gameObject);
            }
        }
    }

    private void OnPlaceBuilding(InputAction.CallbackContext context)
    {
        if (currentGhost != null && !EventSystem.current.IsPointerOverGameObject())
        {
            PlaceObject();
        }
    }

    /// <summary>
    /// Refund resources to inventory. If full, drop on ground.
    /// </summary>
    public void AddResource(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;

        if (InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(item, amount);
            if (added)
            {
                Debug.Log($"[BuildingManager] 자원 환급: {item.itemName} x{amount}");
                return;
            }
        }

        // Inventory full or unavailable → drop on ground
        Debug.LogWarning($"[BuildingManager] 인벤토리 가득 참! {item.itemName} 드랍");
        if (item.itemPrefab != null)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector3 dropPos = transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f;
                Instantiate(item.itemPrefab, dropPos, Quaternion.identity);
            }
        }
    }

    private void CreateGhostMaterials()
    {
        // Try URP shader first, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        // Valid material (Green, transparent)
        ghostMaterialValid = new Material(shader);
        ghostMaterialValid.color = new Color(0f, 1f, 0f, 0.5f);
        SetMaterialTransparent(ghostMaterialValid);

        // Invalid material (Red, transparent)
        ghostMaterialInvalid = new Material(shader);
        ghostMaterialInvalid.color = new Color(1f, 0f, 0f, 0.5f);
        SetMaterialTransparent(ghostMaterialInvalid);
    }

    private void SetMaterialTransparent(Material mat)
    {
        // Set rendering mode to Transparent for URP/Standard
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1); // URP: Transparent
        }
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3); // Standard: Transparent
        }
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void Update()
    {
        // ESC/Cancel logic - using New Input System via Keyboard.current for simple checks if action not bound
        if (isBuildMode && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentGhost != null) CancelBuilding();
            else ToggleBuildMode();
        }

        // Placement mode logic
        if (currentGhost != null)
        {
            HandleGhostPlacement();
        }
    }

    private void HandleGhostPlacement()
    {
        // Raycast for ghost position using New Input System Mouse position
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, buildLayer))
        {
            Vector3 targetPosition = hit.point;

            // Snap detection (Check Shift key via Keyboard.current)
            snapEnabled = !Keyboard.current.leftShiftKey.isPressed;

            if (snapEnabled)
            {
                SnapPoint closestSnap = FindClosestSnapPoint(targetPosition);
                if (closestSnap != null)
                {
                    targetPosition = closestSnap.transform.position;
                }
            }

            currentGhost.transform.position = targetPosition;
        }

        // Update stability preview color
        currentGhost.CalculatePredictedStability();

        // Rotation input (R key)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            rotationAngle += 45f;
            currentGhost.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        }

        // Mouse scroll rotation
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.1f)
        {
            rotationAngle += Mathf.Sign(scroll) * 45f;
            currentGhost.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        }

        // Right click to cancel while placing
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelBuilding();
        }
    }

    public void ToggleBuildMode()
    {
        isBuildMode = !isBuildMode;
        
        // UI 제어
        if (buildingUI) buildingUI.ToggleUI(isBuildMode);

        // 커서 제어 (메뉴 열리면 커서 보이기)
        Cursor.visible = isBuildMode;
        Cursor.lockState = isBuildMode ? CursorLockMode.None : CursorLockMode.Locked;

        Debug.Log($"[BuildingManager] Build Mode: {isBuildMode}");
    }

    public void SelectPiece(BuildingDataSO piece)
    {
        Debug.Log($"[BuildingManager] Selected Piece: {piece.displayName}");
        StartPlacing(piece);
    }

    private void StartPlacing(BuildingDataSO data)
    {
        selectedPiece = data;
        isBuildMode = true;

        // Hide UI
        if (buildingUI) buildingUI.ToggleUI(false);

        // Remove existing ghost
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
        }

        // Create new ghost
        GameObject ghostObj = Instantiate(data.prefab);
        currentGhost = ghostObj.AddComponent<BuildingGhost>();
        currentGhost.Setup(ghostMaterialValid, ghostMaterialInvalid);

        // Reset rotation
        rotationAngle = 0f;
        ghostObj.transform.rotation = Quaternion.identity;

        Debug.Log($"[BuildingManager] Placement mode started for {data.displayName}");
    }

    private void PlaceObject()
    {
        if (currentGhost == null || selectedPiece == null) return;

        // Check if valid placement
        if (currentGhost.isColliding)
        {
            Debug.LogWarning("[BuildingManager] Cannot place - Invalid location (collision detected)");
            BuildingFeedback.Instance?.PlayErrorSound(currentGhost.transform.position);
            return;
        }

        // Check resource cost
        if (!CheckCost(selectedPiece))
        {
            Debug.LogWarning("[BuildingManager] 자원 부족! 건축할 수 없습니다.");
            BuildingFeedback.Instance?.PlayErrorSound(currentGhost.transform.position);
            return;
        }

        // Instantiate the actual building
        Vector3 position = currentGhost.transform.position;
        Quaternion rotation = currentGhost.transform.rotation;
        GameObject newBuilding = Instantiate(selectedPiece.prefab, position, rotation);

        // Add BuildingPiece component for stability tracking and removal
        BuildingPiece piece = newBuilding.AddComponent<BuildingPiece>();
        piece.data = selectedPiece;

        // Calculate stability immediately after placement
        piece.CheckGroundedStatus();
        piece.PropagateStabilityUpdate(new System.Collections.Generic.HashSet<int>());

        // Consume resources after successful placement
        ConsumeCost(selectedPiece);

        Debug.Log($"[BuildingManager] Placed {selectedPiece.displayName} at {position} (Stability: {piece.stability:F1})");

        // Audio & VFX feedback
        BuildingFeedback.Instance?.PlayPlaceSound(position);
        BuildingFeedback.Instance?.SpawnPlaceVFX(position);

        // Keep ghost for continuous building (don't destroy)
        // User can right-click to cancel when done
    }

    private bool CheckCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null || data.constructionCosts.Count == 0)
            return true;

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[BuildingManager] InventorySystem not found!");
            return false;
        }

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && !InventorySystem.Instance.HasItem(cost.item, cost.amount))
                return false;
        }

        return true;
    }

    private void ConsumeCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null || data.constructionCosts.Count == 0)
            return;

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null)
            {
                InventorySystem.Instance.TryConsume(cost.item, cost.amount);
                Debug.Log($"[BuildingManager] 자원 소모: {cost.item.itemName} x{cost.amount}");
            }
        }
    }

    /// <summary>
    /// Find the closest SnapPoint within snap radius, excluding ghost's own snap points.
    /// </summary>
    private SnapPoint FindClosestSnapPoint(Vector3 position)
    {
        // Use OverlapSphere to find all colliders in range
        // Note: SnapPoints don't need colliders, so we search by component instead
        SnapPoint[] allSnapPoints = Object.FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
        
        SnapPoint closestSnap = null;
        float closestDistance = snapRadius;

        foreach (SnapPoint snap in allSnapPoints)
        {
            // Skip if this snap point belongs to the ghost (Ignore Raycast layer)
            if (snap.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
            {
                continue;
            }

            float distance = Vector3.Distance(position, snap.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSnap = snap;
            }
        }

        return closestSnap;
    }

    private void CancelBuilding()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        selectedPiece = null;
        isBuildMode = false;

        // Restore cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[BuildingManager] Building mode cancelled");
    }
}
