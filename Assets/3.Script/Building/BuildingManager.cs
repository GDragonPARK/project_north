using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using StarterAssets;


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
    MustSnap,
    InvalidSnap
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
    private GameObject _snapMarker;  // ★ Target socket marker (yellow bar)
    private GameObject _ghostMarker; // ★ Ghost socket marker (cyan point)

    [Header("Debug")]
    public bool showSnapDebug = true;
    public bool debugFreeBuild = true; // Phase 10.7: 인스펙터에서 즉시 무한 건축 가능한 디버그 토글

    // Diagnostic states
    private string _debugSnapStatus = "None";
    private int _debugTargetCount = 0;
    private float _debugBestDist = 0f;
    private float _debugBestAlign = 0f;
    private float _debugBestScore = 0f;
    private string _debugPlacementStatus = "Valid";
    private string _debugOverlapColliderName = "None";
    private float _debugAlignError = 0f;        // ★ 정합 오차
    private PlacementStatus _lastPlacementStatus = PlacementStatus.Valid;

    // ── Pieces ────────────────────────────────────────────────────────────────
    [Header("Available Pieces")]
    public List<BuildingPieceEntry> availablePieces = new List<BuildingPieceEntry>();
    private int selectedIndex = 0;
    private GameObject _currentGhostInstance;

    // ── Raycast & Snap ────────────────────────────────────────────────────────
    [Header("Raycast & Snap")]
    public LayerMask buildableLayer;
    [SerializeField] private float snapDistance = 1.5f;
    [SerializeField] private float snapRadius   = 1.5f;
    [SerializeField] private LayerMask snapLayer;
    [SerializeField] private float maxRayDistance = 500f;

    // ── Rotation ──────────────────────────────────────────────────────────────
    private int rotationStepIndex = 0; // ★ 90° 스텝 단위 회전 (0,1,2,3)

    // Snap state
    private bool      _isSnapped;
    private Quaternion _snapBaseRot;
    private Transform  _snapTargetSocket;

    // Support state
    private bool _hasSupport;

    // ── NonAlloc & AutoSnap Optimization ──────────────────────────────────────
    private readonly Collider[] _snapColliders = new Collider[128];
    private float _lastBufferWarnTime = -999f;

    // Cutoff constants
    private const float MAX_SNAP_DIST = 0.6f;
    private const float MIN_ALIGN_DOT = 0.7f;
    private const float ALIGN_WEIGHT  = 0.3f;
    private const float VIEW_WEIGHT   = 0.03f; // tie-breaker only

    // Sticky Snap state (히스테리시스)
    private bool      _hasStickySnap;
    private SnapPoint _stickyTarget;
    private SnapPoint _stickyGhost;
    private float     _stickyScore;

    // ★ Stability Lock: Release 경계 안정화
    private bool _snapReleasedThisFrame = false;

    // ★ Stability Lock: MPB 호출 최소화
    private int _prevColorState = -1; // 0=deepGreen, 1=lightGreen, 2=red

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

    // ── 4.15-15: 시각적 보간(스무딩) ──────────────────────────────────────────────
    [Header("Ghost Smoothing")]
    [SerializeField, Range(0.001f, 0.2f)] private float positionSmoothTime = 0.05f;
    [SerializeField, Range(1f, 50f)] private float rotationSmoothSpeed = 25f;
    private Vector3 _smoothVelocity;

    // ── 4.15-13: Placement Fallback (허공 조준 튀는 현상 방지) ────────────────
    [Header("Placement Fallback")]
    private Vector3 _lastValidPos;
    private Quaternion _lastValidRot;

    // ── 4.15-14: GC 없는 두꺼운 충돌 탐색 체계 ─────────────────────────────────
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
    private const float SPHERE_CAST_RADIUS = 0.05f;

    // ── 4.15-12: LayerMask & Ghost Collision Fix ──────────────────────────────
    [Header("Physics & Targeting")]
    [SerializeField] private LayerMask _placementMask; // 보통 Default, Ground, Building 등만 포함
    [SerializeField] private Transform _playerRoot;    // 플레이어 예외 처리용 (필요시 사용)

    [Header("Building Categories")]
    public List<BuildingCategorySO> categories = new List<BuildingCategorySO>();

    // ── Cached indices ────────────────────────────────────────────────────────
    private int _groundLayer;
    private int _buildingLayer;

    // ── 4.15-24A: Magnetic Snap ───────────────────────────────────────────────
    [Header("Magnetic Snap")]
    [SerializeField] private bool enableMagneticSnap = true;
    [SerializeField] private bool enableRotationSnap = true;
    [SerializeField] private bool enableBoundsSnap = true;
    [SerializeField] private bool enableSnapDebug = true;
    private int _snapPointMask;
    private readonly Collider[] _magneticSnapHits = new Collider[64];

    // ── 4.15-26: Bounds Snap Hysteresis ───────────────────────────────────────
    [Header("Bounds Snap Tuning")]
    [SerializeField] private float boundsSnapLockTime = 0.12f;
    [SerializeField] private float boundsSnapEnterDist = 0.25f;
    [SerializeField] private float boundsSnapExitDist = 0.35f;
    [SerializeField] private float boundsEdgeThreshold = 0.35f; // ★ 4.15-28: Edge-Snap threshold

    // ★ 4.15-30: Edge-Snap Hysteresis Variables
    private Collider _lockedEdgeTarget;
    private int _lockedEdgeAxis = -1; // 0: dxMax, 1: dxMin, 2: dzMax, 3: dzMin
    private float _lockMargin = 0.25f;

    private bool _boundsSnapLocked;
    private Collider _lockedTarget;
    private Vector3 _lockedNormal;
    private string _lockedAxis;
    private float _snapLockUntilTime;

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        BuildingManager[] existingManagers = FindObjectsByType<BuildingManager>(FindObjectsSortMode.None);
        if (existingManagers.Length > 1)
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[UI DIAGNOSTICS] Duplicate BuildingManager found on {gameObject.name}. Destroying this duplicate!");
                Destroy(gameObject);
                return;
            }
        }

        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[BuildingManager] Awake -> Instance assigned.");
        }

        buildLayer = LayerMask.GetMask("Ground", "Building", "Terrain");

        // Prepare Audio
        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        // Prepare Target Snap Marker (노란 바)
        _snapMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _snapMarker.name = "TargetMarker";
        Destroy(_snapMarker.GetComponent<Collider>());
        _snapMarker.transform.localScale = Vector3.one * 0.2f;
        var mr = _snapMarker.GetComponent<Renderer>();
        mr.material.color = Color.yellow;
        mr.material.SetFloat("_Glossiness", 0f);
        _snapMarker.transform.SetParent(this.transform);
        _snapMarker.SetActive(false);

        // ★ Prepare Ghost Snap Marker (시안 작은 점)
        _ghostMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ghostMarker.name = "GhostMarker";
        Destroy(_ghostMarker.GetComponent<Collider>());
        _ghostMarker.transform.localScale = Vector3.one * 0.1f;
        var gmr = _ghostMarker.GetComponent<Renderer>();
        gmr.material.color = Color.cyan;
        gmr.material.SetFloat("_Glossiness", 0f);
        _ghostMarker.transform.SetParent(this.transform);
        _ghostMarker.SetActive(false);
        _groundLayer   = LayerMask.NameToLayer("Ground");
        _buildingLayer = LayerMask.NameToLayer("Building");
        _snapPointMask = LayerMask.GetMask("SnapPoint");
        if (buildableLayer.value == 0) buildableLayer = buildLayer;

        CreateGhostMaterials();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        _freeLookCam    = Object.FindFirstObjectByType<Cinemachine.CinemachineFreeLook>();
        _camInputBridge = Object.FindFirstObjectByType<CameraInputBridge>();
        _cameraZoom     = Object.FindFirstObjectByType<CameraZoom>();

        // Ghost prefabs are no longer modified directly in Awake to avoid asset corruption.

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

        // ★ 4.15-2: 숫자키(1~4) 건축 단축키 제거 — 퀵슬롯 충돌 방지
        // 부품 선택은 하단 UI 버튼 클릭(SelectPiece)으로만 수행

        // ★ Fix 1: 스텝 회전 중 스냅 유지 — Sticky가 살아있으면 _isSnapped을 해제하지 않음
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f)       { rotationStepIndex += 1; }
            else if (scroll < -0.1f) { rotationStepIndex -= 1; }
            // 스냅 상태는 Sticky Release 조건에 의해서만 해제됨
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (currentGhost != null) CancelBuilding();
            else ToggleBuildMode();
            return;
        }

        if (isBuildMode) HandleCameraRotation(mouse);
    }

    // ★ 4.15-13: 시네머신과의 프레임 엇박자 지연 해결을 위해 레이캐스트를 LateUpdate로 이동
    void LateUpdate()
    {
        var mouse = Mouse.current;
        if (mouse == null || !isBuildMode) return;
        var cam = Camera.main;
        if (cam == null) return;

        // ★ 4.15-11: UI 관통 설치 방지(이전 버전)가 고스트 이동까지 통째로 차단하던 문제 해결
        // 이곳에 있던 방어벽을 제거하고, 클릭을 실제로 수행하는 하위 로직(ValidateAndPlacePass, HandleDeconstruction)으로 이동시킴
        HandleSimpleGhostPlacement(mouse);
        HandleDeconstruction();
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
        if (ghostGO == null)
        {
            // [Phase 10.2-1] 아무것도 들고 있지 않을 때의 무한 경고 로그 도배 차단 (조용히 리턴)
            return;
        }

        // ★ 4.15-12: 레이캐스트 마스크 통일 (_placementMask 우선, 없으면 fallback)
        int mask = _placementMask.value != 0 ? _placementMask.value : 
                   (buildableLayer.value != 0 ? buildableLayer.value : buildLayer.value);
                   
        Ray ray  = cam.ScreenPointToRay(mouse.position.ReadValue());

        // ★ 디버그 레이저: 씬 뷰에서 레이캐스트 탐색 범위 시각화
        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.yellow);

        // ★ 4.15-14: SphereCastNonAlloc (트리거 무시)
        int hitCount = Physics.SphereCastNonAlloc(ray, SPHERE_CAST_RADIUS, _hitBuffer, maxRayDistance, mask, QueryTriggerInteraction.Ignore);
        
        bool foundValidHit = false;
        RaycastHit bestHit = default;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var h = _hitBuffer[i];
            if (h.collider.isTrigger) continue;
            if (_playerRoot != null && h.collider.transform.root == _playerRoot) continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit = h;
                foundValidHit = true;
            }
        }

        Vector3 targetPos;
        Quaternion targetRot;

        if (foundValidHit)
        {
            if (!ghostGO.activeSelf) ghostGO.SetActive(true);

            // Phase 1: 스냅 판정을 가장 먼저 수행하여 _isSnapped 확정
            targetPos = bestHit.point;
            targetRot = Quaternion.identity;
            ApplySnapAndCorrection(ref targetPos, ref targetRot, ghostGO, bestHit);

            // Phase 2: 엄격한 if-else 분기 (카메라 회전 누수 완벽 차단)
            if (_isSnapped && _snapTargetSocket != null)
            {
                // [오직 스냅 정렬만 수행] — 카메라, hit.normal 일체 참조 금지!
                Transform rootSocket = ghostGO.transform.Find("RootSocket");
                if (rootSocket != null && _snapTargetSocket != null)
                {
                    // 1. 회전 정렬: 타겟 소켓의 월드 회전을 기준으로 고스트의 회전을 역산하여 일치시킴
                    ghostGO.transform.rotation = _snapTargetSocket.rotation * Quaternion.Inverse(rootSocket.localRotation);

                    // 2. 위치 정렬: [핵심] 현재 회전된 고스트의 '루트'와 '소켓' 사이의 '실제 월드 거리'를 구함
                    Vector3 currentWorldOffset = ghostGO.transform.position - rootSocket.position;
                    
                    // 3. 타겟 소켓 위치에 그 월드 거리만큼 더해서 고스트 전체를 한 번에 이동
                    targetPos = _snapTargetSocket.position + currentWorldOffset;
                    targetRot = ghostGO.transform.rotation;
                }
                // RootSocket 없으면 ApplySnapAndCorrection의 결과를 그대로 사용
            }
            else
            {
                // [자유 배치(Free Placement)] — 스냅 실패 시에만 실행
                // ★ 4.15-62: Wall 계열 파묻힘 방지 — hit.point에 높이 오프셋 자동 보정
                targetPos = bestHit.point;
                bool isWallPiece = false;
                if (selectedPiece != null && selectedPiece.displayName != null &&
                    selectedPiece.displayName.ToLower().Contains("wall"))
                {
                    isWallPiece = true;
                }
                else if (availablePieces.Count > 0 && selectedIndex < availablePieces.Count)
                {
                    string pname = availablePieces[selectedIndex].pieceName ?? "";
                    if (pname.ToLower().Contains("wall")) isWallPiece = true;
                }
                if (isWallPiece) targetPos += Vector3.up * 1.5f; // Scale 3.0 기준 절반 높이 보정
                targetRot = Quaternion.Euler(0, rotationStepIndex * 90f, 0);
            }

            // Phase 3: 트랜스폼 적용 및 설치 판정
            ghostGO.transform.position = targetPos;
            ghostGO.transform.rotation = targetRot;

            ValidateAndPlacePass(mouse, ghostGO, bestHit);

            targetPos = ghostGO.transform.position;
            targetRot = ghostGO.transform.rotation;

            _lastValidPos = targetPos;
            _lastValidRot = targetRot;
        }
        else
        {
            // 허공 조준 시 마지막 유효 위치 유지
            if (!ghostGO.activeSelf) ghostGO.SetActive(true);
            _isSnapped  = false;
            _hasSupport = false;
            _snapMarker.SetActive(false);
            _ghostMarker.SetActive(false);

            targetPos = _lastValidPos;
            targetRot = Quaternion.AngleAxis(rotationStepIndex * 90f, Vector3.up);
            
            ApplyGhostSupportFeedback(ghostGO, PlacementStatus.NoSupport);
        }

        // ★ 4.15-15: 최종 보간 적용 (무조건 실행)
        ghostGO.transform.position = Vector3.SmoothDamp(
            ghostGO.transform.position, 
            targetPos, 
            ref _smoothVelocity, 
            positionSmoothTime, 
            Mathf.Infinity, 
            Time.deltaTime
        );

        ghostGO.transform.rotation = Quaternion.Slerp(
            ghostGO.transform.rotation, 
            targetRot, 
            Time.deltaTime * rotationSmoothSpeed
        );
    }

    private void ApplySnapAndCorrection(ref Vector3 pos, ref Quaternion rot, GameObject ghostGO, RaycastHit hit)
    {
        _isSnapped = false;
        _snapTargetSocket = null;
        _stickyGhost = null;

        if (enableMagneticSnap)
        {
            // ★ 5.1-05: 광역 자석 — 반경 2.5f, hit.point + 높이 보정(+0.5f) 이중 탐색
            // 바닥 메시가 Raycast를 가릴 때도 바닥 모서리 SnapPoint를 안정적으로 감지
            Vector3 searchCenter = hit.point + Vector3.up * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(searchCenter, 2.5f, _magneticSnapHits, ~0, QueryTriggerInteraction.Collide);
            
            // [Log Disabled] 매 프레임 콘솔 마비 방지 — 필요 시 enableSnapDebug 주석 해제
            // if (enableSnapDebug)
            // {
            //     Debug.Log($"[SNAP-MAGNET] search={searchCenter:F2} overlap count={count}");
            // }

            
            if (count > 0)
            {
                SnapPoint chosenTarget = null;
                float bestTargetDist = float.MaxValue;

                // 1. 가장 가까운 타겟 소켓 찾기
                for (int i = 0; i < count; i++)
                {
                    var hitCol = _magneticSnapHits[i];

                    // 1. 태그가 SnapPoint가 아니면 무시
                    if (!hitCol.CompareTag("SnapPoint")) continue;

                    // 2. 검색된 소켓이 현재 조종 중인 고스트(ghost)의 자식 객체라면 무시 (자기 자신 스냅 방지)
                    if (hitCol.transform.IsChildOf(ghostGO.transform)) continue;

                    var sp = hitCol.GetComponent<SnapPoint>();
                    if (sp == null) continue;

                    float dist = Vector3.Distance(hit.point, sp.transform.position);
                    if (dist < bestTargetDist)
                    {
                        bestTargetDist = dist;
                        chosenTarget = sp;
                    }
                }

                // 2. 100% 확정 위치/회전 매칭
                if (chosenTarget != null)
                {
                    var ghostPiece = ghostGO.GetComponent<BuildingPiece>();
                    IReadOnlyList<SnapPoint> ghostSockets = ghostPiece != null ? ghostPiece.CachedSockets : null;
                    
                    if (ghostSockets == null || ghostSockets.Count == 0)
                    {
                        var fallback = ghostGO.GetComponentsInChildren<SnapPoint>(true);
                        if (fallback.Length > 0) ghostSockets = fallback;
                    }

                // ★ 5.1-11: Valheim 방식 스냅 — 타겟 건축물 회전 상속 + 소켓 월드 오프셋 포개기
                if (ghostSockets != null && ghostSockets.Count > 0)
                {
                    // --- [Step 1] 고스트 소켓 선택 ---
                    Transform targetParentPre = chosenTarget.transform.parent;
                    Quaternion baseRotPre = (targetParentPre != null)
                        ? targetParentPre.rotation
                        : Quaternion.identity;
                    Quaternion finalRotPre = baseRotPre * Quaternion.Euler(0, rotationStepIndex * 90f, 0);

                    // 0. 마우스 휠 회전 선적용
                    ghostGO.transform.rotation = finalRotPre;

                    string tName = chosenTarget.name.ToLower();
                    string gPrefab = ghostGO.name.ToLower();

                    // 1. 유니티 Transform 불신, '이름' 기반 절대 방향 창조
                    Vector3 GetSemanticDir(string n) {
                        if (n.Contains("_n") || n.Contains("front")) return new Vector3(0, 0, 1);
                        if (n.Contains("_s") || n.Contains("back")) return new Vector3(0, 0, -1);
                        if (n.Contains("_e") || n.Contains("right") || n.Contains("_r")) return new Vector3(1, 0, 0);
                        if (n.Contains("_w") || n.Contains("left") || n.Contains("_l")) return new Vector3(-1, 0, 0);
                        if (n.Contains("top")) return new Vector3(0, 1, 0);
                        if (n.Contains("bot")) return new Vector3(0, -1, 0);
                        return Vector3.forward;
                    }

                    Vector3 tOutward = chosenTarget.transform.parent.rotation * GetSemanticDir(tName);
                    if (tName.Contains("floor")) { tOutward.y = 0; tOutward.Normalize(); }

                    Transform chosenGhost = null;
                    float bestScore = float.MaxValue;

                    // 2. 고스트 자석 스캔 및 채점
                    foreach (var gp in ghostSockets) {
                        if (gp.transform.parent != ghostGO.transform) continue; // 가짜 자석 차단 필터

                        string gName = gp.name.ToLower();
                        bool isValid = false;
                        bool forceMatch = false;

                        // --- 완벽한 타입 필터링 ---
                        if (tName.Contains("floor")) {
                            if (gPrefab.Contains("floor") && gName.Contains("floor")) isValid = true;
                            if (gPrefab.Contains("wall") && gName.Contains("bot")) { isValid = true; forceMatch = true; } // 바닥->벽 크로스
                            if (gPrefab.Contains("roof") && gName.Contains("bot")) { isValid = true; forceMatch = true; } // 바닥->지붕 크로스
                        }
                        else if (tName.Contains("top")) {
                            if (gPrefab.Contains("wall") && gName.Contains("bot")) isValid = true;
                            if (gPrefab.Contains("roof") && gName.Contains("bot")) isValid = true;
                        }
                        else if (tName.Contains("bot")) {
                            if (gPrefab.Contains("wall") && gName.Contains("top")) isValid = true;
                        }
                        else if (tName.Contains("left") || tName.Contains("right")) {
                            if (gPrefab.Contains("wall") && (gName.Contains("left") || gName.Contains("right"))) isValid = true;
                        }
                        else if (tName.Contains("roof")) {
                            if (gPrefab.Contains("roof")) isValid = true; // 지붕->지붕 (forceMatch 없음! 오직 방향으로만 승부)
                        }

                        if (!isValid) continue;

                        // --- 내적(Dot) 채점 ---
                        Vector3 gOutward = finalRotPre * GetSemanticDir(gName);
                        if (gPrefab.Contains("floor")) { gOutward.y = 0; gOutward.Normalize(); }

                        float dot = Vector3.Dot(tOutward, gOutward);

                        // 크로스 결합(수직) 시에만 강제로 최고점 부여
                        if (forceMatch) dot = -1f;

                        // 점수 합산 (바디 매스 시뮬레이션 + 방향 절대 우위)
                        Vector3 simulatedGhostCenter = chosenTarget.transform.position - (gp.transform.position - ghostGO.transform.position);
                        float distToHit = Vector3.Distance(simulatedGhostCenter, hit.point);
                        float score = (dot * 100f) + distToHit;

                        if (score < bestScore) {
                            bestScore = score;
                            chosenGhost = gp.transform;
                        }
                    }

                    if (chosenGhost == null) chosenGhost = ghostSockets[0].transform;

                    // 3. 밀어내기 없는 1:1 완벽 포개기 (오차율 0%)
                    Vector3 finalOffset = chosenGhost.position - ghostGO.transform.position;
                    pos = chosenTarget.transform.position - finalOffset;

                    // [절대 회전 방어선]
                    // 베이스 스크립트의 억지 방향 정렬(Auto-Align)을 무력화하고, 유저의 휠 회전(finalRotPre)을 100% 영구 보존합니다.
                    rot = finalRotPre;
                    ghostGO.transform.rotation = finalRotPre;
                    ghostGO.transform.position = pos;

                    // [Log Disabled] 매 프레임 콘솔 마비 방지 — 필요 시 enableSnapDebug 주석 해제
                    // if (enableSnapDebug)
                    // {
                    //     Debug.Log($"[VALHEIM-SNAP] target={chosenTarget.name} mySocket={chosenGhost.name} pos={pos:F3} rot={rot.eulerAngles:F1}");
                    // }

                    _isSnapped = true;
                    _snapTargetSocket = chosenTarget.transform;
                    _stickyGhost = chosenGhost.GetComponent<SnapPoint>();
                    return;
                }
                }
            }
        }
    }

    private void BasePlacementPass(RaycastHit hit, GameObject ghostGO)
    {
        if (!ghostGO.activeSelf) ghostGO.SetActive(true);

        // Base rotation from user input (90° steps)
        Quaternion ghostRot = Quaternion.AngleAxis(rotationStepIndex * 90f, Vector3.up);
        ghostGO.transform.rotation = ghostRot;

        // ★ Fix 2: Snap Release 직후 1프레임은 Anchor 보정 스킵 (위치 튐 방지)
        if (_snapReleasedThisFrame)
        {
            _snapReleasedThisFrame = false;
            _isSnapped = false;
            // ★ 4.15-22: 이전 위치 유지 대신 hit.point를 직접 대입 (고정 버그 방지)
            ghostGO.transform.position = hit.point;
            return;
        }

        // PlacementAnchor 기반 지면 보정 (스냅 전 자유 배치에만 적용)
        Vector3 targetPos = hit.point;
        Transform anchor = ghostGO.transform.Find("PlacementAnchor");
        if (anchor != null)
        {
            Vector3 anchorWorldOffset = ghostRot * anchor.localPosition;
            if (float.IsNaN(anchorWorldOffset.x) || float.IsNaN(anchorWorldOffset.y) || float.IsNaN(anchorWorldOffset.z))
            {
                Debug.LogWarning("[BuildingManager] Anchor offset is NaN! Using zero.");
                anchorWorldOffset = Vector3.zero;
            }
            targetPos = hit.point - anchorWorldOffset;
        }

        // ★ 4.15-22: 직접 대입 (Lerp 제거 — 스무딩은 HandleSimpleGhostPlacement 끝에서 SmoothDamp로 처리)
        ghostGO.transform.position = targetPos;
        
        _isSnapped = false;
    }

    private void AutoSnapPass(GameObject ghostGO)
    {
        // Reset debug vars
        _debugSnapStatus  = "Searching...";
        _debugTargetCount = 0;
        _debugBestDist    = 0f;
        _debugBestAlign   = 0f;
        _debugBestScore   = 0f;

        // 1. Alt = 자유 배치
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
        {
            ReleaseStickySnap();
            _debugSnapStatus = "FreePlace (Alt)";
            return;
        }

        // ── 2. NonAlloc 볼류메트릭 검색 ────────────────────────────────────────
        int hitCount = Physics.OverlapSphereNonAlloc(
            ghostGO.transform.position, MAX_SNAP_DIST, _snapColliders, 1 << _buildingLayer, QueryTriggerInteraction.Collide);

        // ★ Fix 4: 버퍼 포화 → 반경 축소 재시도
        if (hitCount >= _snapColliders.Length)
        {
            if (Time.time - _lastBufferWarnTime > 1f)
            {
                Debug.LogWarning($"[AutoSnap] Collider buffer full ({_snapColliders.Length}). Retrying with reduced radius.");
                _lastBufferWarnTime = Time.time;
            }
            // 반경 50%로 축소하여 재시도 — 가까운 후보 우선
            hitCount = Physics.OverlapSphereNonAlloc(
                ghostGO.transform.position, MAX_SNAP_DIST * 0.5f, _snapColliders, 1 << _buildingLayer, QueryTriggerInteraction.Collide);
        }

        if (hitCount == 0)
        {
            ReleaseStickySnap();
            _debugSnapStatus = "No Buildings Nearby";
            return;
        }

        // ── 3. Ghost 소켓 캐싱 조회 ──────────────────────────────────────────
        var ghostPiece = ghostGO.GetComponent<BuildingPiece>();
        IReadOnlyList<SnapPoint> ghostSockets = ghostPiece != null
            ? ghostPiece.CachedSockets : null;

        // 캐시가 없으면 fallback (최초 1회만 발생 가능)
        if (ghostSockets == null || ghostSockets.Count == 0)
        {
            var fallback = ghostGO.GetComponentsInChildren<SnapPoint>(true);
            if (fallback.Length == 0)
            {
                ReleaseStickySnap();
                _debugSnapStatus = "Ghost Missing Sockets";
                return;
            }
            ghostSockets = fallback;
        }

        // ── 4. 카메라 뷰 방향 (tie-breaker용) ─────────────────────────────────
        Vector3 camFwd = Vector3.forward;
        var cam = Camera.main;
        if (cam != null) camFwd = cam.transform.forward;

        // ── 5. Sticky Snap 해제 조건 체크 ──────────────────────────────────────
        if (_hasStickySnap && _stickyTarget != null && _stickyGhost != null)
        {
            Vector3 stickyGhostWorldPos = ghostGO.transform.TransformPoint(_stickyGhost.transform.localPosition);
            float stickyDist = Vector3.Distance(_stickyTarget.transform.position, stickyGhostWorldPos);

            Vector3 stickyGhostFwd = ghostGO.transform.TransformDirection(_stickyGhost.transform.forward);
            float stickyAlign = Vector3.Dot(-_stickyTarget.transform.forward, stickyGhostFwd);

            float releaseDist  = MAX_SNAP_DIST * 1.2f;
            float releaseAlign = MIN_ALIGN_DOT - 0.1f;

            if (stickyDist > releaseDist || stickyAlign < releaseAlign || _stickyTarget.isOccupied)
            {
                ReleaseStickySnap();
            }
        }

        // ── 6. 전체 후보 스코어링 (Cutoff → Score → Best) ─────────────────────
        SnapPoint bestTarget = null;
        SnapPoint bestGhost  = null;
        float bestScore = float.MaxValue;
        float bestDist  = float.MaxValue;
        float bestAlignForDebug = 0f;
        int totalTargetSockets  = 0;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _snapColliders[i];
            if (col == null) continue;

            // BuildingPiece 캐싱된 소켓 사용
            var piece = col.GetComponent<BuildingPiece>();
            if (piece == null) piece = col.GetComponentInParent<BuildingPiece>();
            if (piece == null) continue;

            var targetSockets = piece.CachedSockets;
            if (targetSockets == null) continue;
            totalTargetSockets += targetSockets.Count;

            for (int t = 0; t < targetSockets.Count; t++)
            {
                var target = targetSockets[t];
                if (target == null || target.isOccupied) continue;

                for (int g = 0; g < ghostSockets.Count; g++)
                {
                    var ghost = ghostSockets[g];
                    if (!ghost.CanConnectTo(target)) continue;

                    // Depth-1 검증
                    if (ghost.transform.parent != ghostGO.transform) continue;

                    Vector3 ghostWorldPos = ghostGO.transform.TransformPoint(ghost.transform.localPosition);
                    Vector3 ghostWorldFwd = ghostGO.transform.TransformDirection(ghost.transform.forward);

                    // ★ Cutoff 먼저 (점수 계산 전)
                    float dist = Vector3.Distance(target.transform.position, ghostWorldPos);
                    if (dist > MAX_SNAP_DIST) continue;

                    float alignDot = Vector3.Dot(-target.transform.forward, ghostWorldFwd);
                    if (alignDot < MIN_ALIGN_DOT) continue;

                    // ★ 스코어링: dist + (1-align)*weight + (1-view)*viewWeight
                    float align01 = Mathf.Clamp01(alignDot);
                    Vector3 toTarget = (target.transform.position - ghostGO.transform.position).normalized;
                    float view01 = Mathf.Clamp01(Vector3.Dot(camFwd, toTarget) * 0.5f + 0.5f);

                    float score = dist + (1f - align01) * ALIGN_WEIGHT + (1f - view01) * VIEW_WEIGHT;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestDist  = dist;
                        bestAlignForDebug = alignDot;
                        bestTarget = target;
                        bestGhost  = ghost;
                    }
                }
            }
        }

        _debugTargetCount = totalTargetSockets;
        _debugBestScore   = bestScore;
        _debugBestDist    = bestDist;
        _debugBestAlign   = bestAlignForDebug;

        // ── 7. ★ Fix 3: Sticky Snap 히스테리시스 (분리된 메서드) ──────────────
        EvaluateStickyHysteresis(ref bestTarget, ref bestGhost, bestScore);

        // null 안전
        if (bestTarget == null || bestGhost == null)
        {
            ReleaseStickySnap();
            _debugSnapStatus = "NoCompatible";
            return;
        }

        // ── 8. 3단계 정합 공식 적용 ────────────────────────────────────────────
        Transform targetSocket = bestTarget.transform;
        Transform ghostSocket  = bestGhost.transform;

        Vector3 upVector = (bestTarget.snapType == SnapType.Roof)
            ? targetSocket.up : Vector3.up;

        Quaternion baseRot  = Quaternion.LookRotation(-targetSocket.forward, upVector);
        Quaternion stepRot  = Quaternion.AngleAxis(rotationStepIndex * 90f, upVector);
        Quaternion finalRot = stepRot * baseRot * Quaternion.Inverse(ghostSocket.localRotation);
        Vector3 snappedPos  = targetSocket.position - (finalRot * ghostSocket.localPosition);

        ghostGO.transform.position = snappedPos;
        ghostGO.transform.rotation = finalRot;

        _isSnapped        = true;
        _snapBaseRot      = finalRot;
        _snapTargetSocket = targetSocket;

        _debugSnapStatus = "Snapped!";
    }

    /// <summary>
    /// ★ Fix 3: Sticky Snap 히스테리시스 평가 (가독성 분리)
    /// - 기존 후보 유지 우선
    /// - 새 후보는 15% 이상 좋아야 교체
    /// - 후보 없으면 기존 Sticky 유지
    /// </summary>
    private void EvaluateStickyHysteresis(ref SnapPoint bestTarget, ref SnapPoint bestGhost, float bestScore)
    {
        bool hasNewCandidate = (bestTarget != null && bestGhost != null);

        if (hasNewCandidate && _hasStickySnap)
        {
            // ── 교체 판정: 기존보다 15% 이상 좋아야 교체 ──
            if (bestScore < _stickyScore * 0.85f)
            {
                _stickyTarget = bestTarget;
                _stickyGhost  = bestGhost;
                _stickyScore  = bestScore;
            }
            // ── 기존 유지 ──
            bestTarget = _stickyTarget;
            bestGhost  = _stickyGhost;
        }
        else if (hasNewCandidate && !_hasStickySnap)
        {
            // ── 첫 스냅 등록 ──
            _hasStickySnap = true;
            _stickyTarget  = bestTarget;
            _stickyGhost   = bestGhost;
            _stickyScore   = bestScore;
        }
        else if (!hasNewCandidate && _hasStickySnap)
        {
            // ── 새 후보 없음 → 기존 Sticky 유지 ──
            bestTarget = _stickyTarget;
            bestGhost  = _stickyGhost;
        }
        // else: 후보도 없고 Sticky도 없음 → bestTarget/bestGhost는 null 유지
    }

    /// <summary>
    /// Sticky Snap 해제: 상태 초기화 + Release 경계 플래그 설정
    /// </summary>
    private void ReleaseStickySnap()
    {
        _hasStickySnap = false;
        _stickyTarget  = null;
        _stickyGhost   = null;
        _stickyScore   = float.MaxValue;
        _snapReleasedThisFrame = true; // ★ Fix 2: 1프레임 안정화
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
                // [Phase 6.1-3] 상하 시야각 확장: 배율 상향(0.01f→0.0035f), Clamp01 유지하되 민감도 개선
                _freeLookCam.m_YAxis.Value -= delta.y * (cameraRotationSpeedMultiplier * 0.0035f);
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
            // ★ 4.15-11: UI 위 철거 클릭(우클릭) 완벽 방어
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            
            // ★ 4.15-14: 철거 레이캐스트도 SphereCast로 통일 (건물을 빗맞추는 현상 완화)
            int hitCount = Physics.SphereCastNonAlloc(ray, SPHERE_CAST_RADIUS, _hitBuffer, 500f, 1 << _buildingLayer, QueryTriggerInteraction.Ignore);
            
            bool foundValidHit = false;
            RaycastHit bestHit = default;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var h = _hitBuffer[i];
                if (h.collider.isTrigger) continue;
                if (_playerRoot != null && h.collider.transform.root == _playerRoot) continue;

                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    bestHit = h;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                // Find root object in case we hit a child snap point or sub-collider
                GameObject targetObj = bestHit.collider.gameObject;
                
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
                if (_ghostMarker) _ghostMarker.SetActive(false);
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
        // --- 0. ★ 듀얼 마커 업데이트 ---
        if (_isSnapped && _snapTargetSocket != null && _stickyGhost != null)
        {
            _snapMarker.SetActive(true);
            _snapMarker.transform.position = _snapTargetSocket.position;
            _snapMarker.transform.rotation = _snapTargetSocket.rotation;

            // Ghost마커: 스냅된 ghost socket의 월드 위치
            Vector3 ghostSocketWorld = ghostGO.transform.TransformPoint(_stickyGhost.transform.localPosition);
            _ghostMarker.SetActive(true);
            _ghostMarker.transform.position = ghostSocketWorld;

            // ★ 정합 오차 계산
            _debugAlignError = Vector3.Distance(_snapTargetSocket.position, ghostSocketWorld);
        }
        else
        {
            _snapMarker.SetActive(false);
            _ghostMarker.SetActive(false);
            _debugAlignError = 0f;
        }

        // --- 1. Validation ---
        PlacementStatus status = ValidatePlacement(ghostGO, hit);
        _lastPlacementStatus = status;
        bool isValid = (status == PlacementStatus.Valid);

        _hasSupport = isValid;

        // --- 2. Update Debug State ---
        _debugPlacementStatus = status.ToString();
        if (status != PlacementStatus.Overlap) _debugOverlapColliderName = "None";

        // --- 3. ★ 3단 컨러 피드백 ---
        ApplyGhostColorFeedback(ghostGO, status, _isSnapped);

        // --- 4. Placement ---
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // If hovering over UI (e.g. inventory or debug panels), block placement
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (isValid)
            {
                // [Phase 11.5] 건축 스태미나 소모 (건축 1회당 10 소모)
                if (CharacterStats.Instance != null && !debugFreeBuild)
                {
                    if (!CharacterStats.Instance.ConsumeStamina(10f))
                    {
                        Debug.LogWarning("<color=orange>[Building]</color> 스태미나가 부족하여 건축할 수 없습니다!");
                        return;
                    }
                }

                var real = GetCurrentRealPrefab();
                if (real != null)
                {
                    // [Phase 9.1] 자원 비용 검사 (Phase 10.7 디버그 무료 버전 추가)
                    var bp = real.GetComponent<BuildingPiece>();
                    if (bp != null && InventorySystem.Instance != null && !debugFreeBuild)
                    {
                        string requiredItem   = bp.requiredItemName;
                        int    requiredAmount = bp.requiredAmount;

                        if (!InventorySystem.Instance.HasItem(requiredItem, requiredAmount))
                        {
                            Debug.LogWarning($"<color=red>[Building]</color> 자원이 부족합니다! 필요 자원: {requiredItem} x{requiredAmount}");
                            return;
                        }
                        InventorySystem.Instance.ConsumeItem(requiredItem, requiredAmount);
                    }
                    else if (debugFreeBuild)
                    {
                        // 자원 소모 없이 통과
                        //Debug.Log($"[BuildingManager] (디버그 모드) {real.name} 자원 소모 무효화");
                    }

                    var newObj = Instantiate(real, ghostGO.transform.position, ghostGO.transform.rotation);
                    Debug.Log($"[BuildingManager] Placed {real.name} @ {ghostGO.transform.position}");

                    // [Phase 8.3] 렌치(Hammer) 장착 시 한 손 건축 애니메이션 재생
                    if (_playerRoot != null)
                    {
                        var tpc = _playerRoot.GetComponent<ThirdPersonController>();
                        tpc?.PlayBuildAnimation();
                    }

                    // [Phase 6.1-5] BuildingStability 통합 완료 — BuildingPiece.CalculateAndShowStability() 직접 호출
                    var piece = newObj.GetComponent<BuildingPiece>();
                    if (piece != null) piece.CalculateAndShowStability();

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

        // ★ 4.15-23: 지형/건물만 검사 (Default 제거 — 플레이어 충돌 방지)
        int supportMask = LayerMask.GetMask("Ground", "Building", "Terrain");
        Collider[] hits = Physics.OverlapBox(center, halfExt, ghostGO.transform.rotation, supportMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
            if (col.transform.IsChildOf(ghostGO.transform)) continue;
            // ★ 4.15-23: 플레이어 본체 무시
            if (_playerRoot != null && col.transform.root == _playerRoot) continue;

            int layer = col.gameObject.layer;
            if (layer == _groundLayer || layer == _buildingLayer ||
                layer == LayerMask.NameToLayer("Terrain"))
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
    /// ★ 3단 컨러 피드백: 스냅+Valid=진한초록, 비스냅+Valid=연한초록, Invalid=빨강
    /// MaterialPropertyBlock 기반으로 GC 0, 인스턴싱 이슈 없음.
    /// </summary>
    private void ApplyGhostColorFeedback(GameObject ghostGO, PlacementStatus status, bool isSnapped)
    {
        // ★ Fix 5: 상태가 바뀌었을 때만 SetPropertyBlock 호출
        int newState = (status == PlacementStatus.Valid) ? (isSnapped ? 0 : 1) : 2;
        if (newState == _prevColorState) return;
        _prevColorState = newState;

        Renderer[] renderers = ghostGO.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Color color;
        switch (newState)
        {
            case 0:  color = new Color(0f, 0.9f, 0f, 0.6f);    break; // 진한 초록 (Snapped+Valid)
            case 1:  color = new Color(0.3f, 0.8f, 0.3f, 0.4f); break; // 연한 초록 (Unsnapped+Valid)
            default: color = new Color(1f, 0.2f, 0.2f, 0.5f);   break; // 빨강 (Invalid)
        }

        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(_mpb);
            if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_BaseColor"))
                _mpb.SetColor(ColorProp, color);
            else
                _mpb.SetColor("_Color", color);
            rend.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>
    /// Returns true if the ghost overlaps another Building collider (penetration).
    /// Uses a slightly SHRUNK bounding box to allow edge-touching without triggering.
    /// </summary>
    private bool CheckOverlap(GameObject ghostGO)
    {
        _debugOverlapColliderName = "None";

        Renderer rend = ghostGO.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        Bounds  bounds  = rend.bounds;
        Vector3 center  = bounds.center;
        Vector3 halfExt = bounds.extents * 0.70f;
        if (halfExt.x < 0.01f) halfExt.x = 0.01f;
        if (halfExt.y < 0.01f) halfExt.y = 0.01f;
        if (halfExt.z < 0.01f) halfExt.z = 0.01f;

        // ★ 4.15-23: 트리거 무시 추가
        int buildMask = LayerMask.GetMask("Building");
        Collider[] hits = Physics.OverlapBox(center, halfExt, ghostGO.transform.rotation, buildMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (col.transform.IsChildOf(ghostGO.transform)) continue;
            if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
            // ★ 4.15-23: 플레이어 본체 무시
            if (_playerRoot != null && col.transform.root == _playerRoot) continue;
            
            _debugOverlapColliderName = col.name;
            return true;
        }
        return false;
    }



    // ═══════════════════════════════════════════════════════════════════════════
    // Debug GUI
    // ═══════════════════════════════════════════════════════════════════════════

    // ★ Fix 6: Release 빌드에서 HUD 미표시
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!showSnapDebug || !isBuildMode) return;

        Rect rect = new Rect(10, Screen.height / 2 - 120, 320, 260);
        GUI.Box(rect, "Build Debug Panel");

        GUILayout.BeginArea(new Rect(20, Screen.height / 2 - 100, 300, 240));

        // ★ 상태 요약
        GUI.color = _lastPlacementStatus == PlacementStatus.Valid ? Color.green : Color.red;
        GUILayout.Label($"Status: {_lastPlacementStatus}  |  Snapped: {_isSnapped}");
        GUI.color = Color.white;

        // 소켓 카운트
        var currentGhost = GetCurrentGhostPrefab();
        int ghostSocketCount = 0;
        if (currentGhost != null)
        {
            var bp = currentGhost.GetComponent<BuildingPiece>();
            ghostSocketCount = bp != null ? bp.CachedSockets.Count
                : currentGhost.GetComponentsInChildren<SnapPoint>(true).Length;
        }
        GUILayout.Label($"Ghost Sockets: {ghostSocketCount}  |  Target Sockets: {_debugTargetCount}");

        // 스냅 상세
        GUILayout.Label($"Snap: {_debugSnapStatus}");
        GUILayout.Label($"Dist: {_debugBestDist:F3}  |  Align: {_debugBestAlign:F3}  |  Score: {_debugBestScore:F3}");

        // ★ 정합 오차 (스냅 성공 시)
        if (_isSnapped)
        {
            GUI.color = _debugAlignError > 0.01f ? Color.yellow : Color.green;
            GUILayout.Label($"Align Error: {_debugAlignError:F4}m{(_debugAlignError > 0.01f ? " ⚠ HIGH" : " ✓")}");
            GUI.color = Color.white;
        }

        // 배치 상태
        if (_debugPlacementStatus != "Valid")
        {
            GUI.color = Color.red;
            GUILayout.Label($"Place: {_debugPlacementStatus}");
            if (_debugPlacementStatus == "Overlap")
                GUILayout.Label($"  Overlap: {_debugOverlapColliderName}");
            GUI.color = Color.white;
        }

        GUILayout.EndArea();
    }
#endif

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
        SnapPoint[] ghostSockets = ghostGO.GetComponentsInChildren<SnapPoint>(true);
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

        // ★ 3단계 정합 (Legacy path)
        Vector3 legacyUp = (bestTarget.snapType == SnapType.Roof)
            ? bestTarget.transform.up : Vector3.up;
        Quaternion legacyStep = Quaternion.AngleAxis(rotationStepIndex * 90f, legacyUp);
        Quaternion finalRot = legacyStep * snapRot * Quaternion.Inverse(bestGhostSocket.transform.localRotation);

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

    private void SpawnGhost()
    {
        if (_currentGhostInstance != null)
        {
            Destroy(_currentGhostInstance);
            _currentGhostInstance = null;
        }

        if (availablePieces == null || availablePieces.Count == 0) return;
        var asset = availablePieces[selectedIndex].ghostPrefab;
        if (asset != null)
        {
            _currentGhostInstance = Instantiate(asset);
            _currentGhostInstance.name = asset.name.Replace("_Instance", "") + "_Instance";
            _currentGhostInstance.transform.rotation = Quaternion.identity;
            
            SafeLockGhost(_currentGhostInstance);
            _currentGhostInstance.SetActive(true);
            
            foreach (var r in _currentGhostInstance.GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = true;

            // ★ 스무딩 변수 초기화 — 새 고스트가 즉시 마우스 위치로 이동
            _smoothVelocity = Vector3.zero;
            _lastValidPos = _currentGhostInstance.transform.position;
            _lastValidRot = Quaternion.identity;
            _prevColorState = -1;

            Debug.Log($"[BuildingManager] 씬에 고스트 생성 완료: {_currentGhostInstance.name}");
        }
    }

    public void SelectPiece(int index)
    {
        if (availablePieces == null || availablePieces.Count == 0) return;
        index = Mathf.Clamp(index, 0, availablePieces.Count - 1);

        selectedIndex = index;
        rotationStepIndex = 0;
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
            SpawnGhost();
        }
        Debug.Log($"[BuildingManager] Piece [{index}]: {availablePieces[index].pieceName}");
    }

    public void ToggleBuildMode()
    {
        bool previousState = isBuildMode;
        isBuildMode = !isBuildMode;
        
        Debug.Log($"[UI DIAGNOSTICS] ToggleBuildMode Called. Previous: {previousState}, New: {isBuildMode}");
        
        if (buildingUI) 
        {
            Debug.Log($"[UI DIAGNOSTICS] buildingUI is NOT null. Calling buildingUI.ToggleUI({isBuildMode})");
            buildingUI.ToggleUI(isBuildMode);
        }
        else
        {
            Debug.LogWarning("[UI DIAGNOSTICS] buildingUI reference is NULL!");
        }

        if (isBuildMode)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.Confined;
            if (_camInputBridge) _camInputBridge.enabled = false;
            if (_cameraZoom)     _cameraZoom.enabled     = false;
            
            if (buildingUIPanel != null) 
            {
                Debug.Log($"[UI DIAGNOSTICS] buildingUIPanel is NOT null. Current activeSelf: {buildingUIPanel.activeSelf}. Setting to TRUE.");
                buildingUIPanel.SetActive(true);
                Debug.Log($"[UI DIAGNOSTICS] buildingUIPanel activeSelf after SetActive: {buildingUIPanel.activeSelf}");
            }
            else
            {
                Debug.LogWarning("[UI DIAGNOSTICS] buildingUIPanel reference is NULL!");
            }
        }
        else
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (_camInputBridge) _camInputBridge.enabled = true;
            if (_cameraZoom)     _cameraZoom.enabled     = true;
            
            if (_currentGhostInstance != null)
            {
                Destroy(_currentGhostInstance);
                _currentGhostInstance = null;
            }
            
            _isSnapped = false;
            
            if (buildingUIPanel != null) 
            {
                Debug.Log($"[UI DIAGNOSTICS] buildingUIPanel is NOT null. Setting to FALSE.");
                buildingUIPanel.SetActive(false);
            }
            
            if (_snapMarker != null) _snapMarker.SetActive(false);
            if (_ghostMarker != null) _ghostMarker.SetActive(false);
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

    private GameObject GetCurrentGhostPrefab() => _currentGhostInstance;

    private GameObject GetCurrentRealPrefab() =>
        (availablePieces != null && availablePieces.Count > 0)
            ? availablePieces[selectedIndex].realPrefab : null;

    private void ApplyRotationToCurrentGhost()
    {
        Quaternion rot;
        if (_isSnapped && _snapTargetSocket != null)
            rot = Quaternion.AngleAxis(rotationStepIndex * 90f, _snapTargetSocket.up) * Quaternion.identity;
        else
            rot = Quaternion.AngleAxis(rotationStepIndex * 90f, Vector3.up);

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
        rotationStepIndex = 0;
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
        bp.CalculateAndShowStability();
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
        if (_currentGhostInstance != null) { Destroy(_currentGhostInstance); _currentGhostInstance = null; }
        selectedPiece = null; isBuildMode = false; _isSnapped = false;
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
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

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private Bounds CalculateGhostWorldBounds(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            return new Bounds(ghost.transform.position, Vector3.zero);
        }

        Bounds b = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            b.Encapsulate(colliders[i].bounds);
        }
        return b;
    }

    // ★ 4.15-12: 고스트 무적화(투명화) 처리
    // 고스트가 레이캐스트를 스스로 막거나 플레이어를 밀어내는 현상을 원천 방어합니다.
    private void SafeLockGhost(GameObject ghostObj)
    {
        if (ghostObj == null) return;
        
        // 1. 모든 Collider 강제 비활성화
        var colliders = ghostObj.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            c.enabled = false;
        }

        // 2. 자신과 자식들의 레이어를 Ignore Raycast(2)로 강제 변경
        Transform[] allChildren = ghostObj.GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            t.gameObject.layer = 2; // Ignore Raycast
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Visual Debugging: 소켓 위치를 씬(Scene) 창에서 눈으로 확인
    // ═══════════════════════════════════════════════════════════════════════════
    private void OnDrawGizmos()
    {
        // 타겟 소켓 그리기 (파란색 구 + 시안색 방향선)
        if (_isSnapped && _snapTargetSocket != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_snapTargetSocket.position, 0.2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_snapTargetSocket.position, _snapTargetSocket.forward * 1f);
        }

        // 고스트의 RootSocket 그리기 (빨간색 구)
        var ghostGO = GetCurrentGhostPrefab();
        if (ghostGO != null)
        {
            Transform rootSocket = ghostGO.transform.Find("RootSocket");
            if (rootSocket != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(rootSocket.position, 0.2f);
            }
        }
    }
}
