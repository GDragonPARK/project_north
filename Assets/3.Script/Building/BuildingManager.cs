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

    [Header("Resource System (Debug)")]
    [SerializeField] private int debugWoodAmount = 100;

    [Header("Input System")]
    private PlayerInput playerInput;
    private InputAction toggleBuildAction;
    private InputAction removeAction;

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
    }

    void OnDisable()
    {
        if (toggleBuildAction != null) toggleBuildAction.Disable();
        if (removeAction != null) removeAction.Disable();
    }

    private void SetupInputActions()
    {
        playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("[BuildingManager] PlayerInput not found in scene!");
            return;
        }

        // Get actions from Input System
        toggleBuildAction = playerInput.actions["ToggleBuildMenu"];
        removeAction = playerInput.actions["Remove"];

        // Subscribe to events
        if (toggleBuildAction != null)
        {
            toggleBuildAction.performed += OnToggleBuildMenu;
        }

        if (removeAction != null)
        {
            removeAction.performed += OnRemoveBuilding;
        }
    }

    private void OnToggleBuildMenu(InputAction.CallbackContext context)
    {
        ToggleBuildMode();
    }

    private void OnRemoveBuilding(InputAction.CallbackContext context)
    {
        // Only remove in build mode
        if (!isBuildMode) return;

        // Raycast to find building
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            BuildingPiece piece = hit.collider.GetComponent<BuildingPiece>();
            if (piece != null)
            {
                // Refund resources
                piece.RefundResources();
                
                Debug.Log($"[BuildingManager] Removed building: {hit.collider.gameObject.name}");
                
                // Destroy building
                Destroy(hit.collider.gameObject);
            }
        }
    }

    /// <summary>
    /// Add resource to the debug inventory (for building system)
    /// </summary>
    public void AddResource(string itemName, int amount)
    {
        if (itemName == "Wood")
        {
            debugWoodAmount += amount;
            Debug.Log($"[BuildingManager] 자원 획득! {itemName} (+{amount}) | 현재: {debugWoodAmount}");
        }
        // Add more resource types as needed
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
        // ESC to close build mode
        if (isBuildMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleBuildMode();
        }

        // Placement mode logic
        if (currentGhost != null)
        {
            HandleGhostPlacement();
        }
    }

    private void HandleGhostPlacement()
    {
        // Raycast for ghost position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, buildLayer))
        {
            Vector3 targetPosition = hit.point;

            // Snap detection (unless Shift is held)
            snapEnabled = !Input.GetKey(KeyCode.LeftShift);

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

        // Rotation input
        if (Input.GetKeyDown(KeyCode.R))
        {
            rotationAngle += 45f;
            currentGhost.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        }

        // Mouse scroll rotation
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            rotationAngle += scroll * 45f;
            currentGhost.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        }

        // Place object (Left Click)
        if (Input.GetMouseButtonDown(0))
        {
            // Check if pointer is over UI
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                PlaceObject();
            }
        }

        // Cancel (Right Click)
        if (Input.GetMouseButtonDown(1))
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
            return;
        }

        // Check resource cost
        if (!CheckCost(selectedPiece))
        {
            Debug.LogWarning("[BuildingManager] 자원 부족! 건축할 수 없습니다.");
            return;
        }

        // Instantiate the actual building
        Vector3 position = currentGhost.transform.position;
        Quaternion rotation = currentGhost.transform.rotation;
        GameObject newBuilding = Instantiate(selectedPiece.prefab, position, rotation);

        // Add BuildingPiece component for stability tracking and removal
        BuildingPiece piece = newBuilding.AddComponent<BuildingPiece>();
        piece.data = selectedPiece; // Store reference for resource refund

        // Consume resources after successful placement
        ConsumeCost(selectedPiece);

        Debug.Log($"[BuildingManager] Placed {selectedPiece.displayName} at {position}");
        Debug.Log($"[BuildingManager] 남은 나무: {debugWoodAmount}");

        // Keep ghost for continuous building (don't destroy)
        // User can right-click to cancel when done
    }

    private bool CheckCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null || data.constructionCosts.Count == 0)
        {
            return true; // No cost required
        }

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && cost.item.itemName == "Wood")
            {
                if (debugWoodAmount < cost.amount)
                {
                    return false;
                }
            }
            // Add more resource types as needed
        }

        return true;
    }

    private void ConsumeCost(BuildingDataSO data)
    {
        if (data.constructionCosts == null || data.constructionCosts.Count == 0)
        {
            return;
        }

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && cost.item.itemName == "Wood")
            {
                debugWoodAmount -= cost.amount;
            }
            // Add more resource types as needed
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
