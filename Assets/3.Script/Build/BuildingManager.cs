using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Blueprints")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject stoneWallPrefab;
    public GameObject workbenchPrefab;

    [Header("Settings")]
    public float maxBuildDistance = 10f;
    public float snapRadius = 1.0f;
    public LayerMask buildableLayers;
    public int woodCost = 10;
    public int stoneCost = 15;
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI warningText;

    [Header("Materials")]
    public Material ghostValidMat;
    public Material ghostInvalidMat;

    private GameObject m_currentGhost;
    private GameObject m_currentPrefab;
    private bool m_isBuilding;
    private float m_rotationY;

    [Header("Refinement")]
    public AudioSource audioSource;
    public AudioClip buildSound;
    public int refundAmount = 5;
    public Animator playerAnimator; // Assign manually or find

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (buildableLayers == 0) buildableLayers = LayerMask.GetMask("Default", "Terrain");
        
        // Runtime Material Generation if null
        if (ghostValidMat == null) CreateGhostMaterial(true);
        if (ghostInvalidMat == null) CreateGhostMaterial(false);

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (playerAnimator == null) 
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerAnimator = p.GetComponent<Animator>();
        }
    }

    private void CreateGhostMaterial(bool isValid)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        Color color = isValid ? Color.cyan : Color.red;
        color.a = 0.5f;
        mat.color = color;

        if (isValid) ghostValidMat = mat;
        else ghostInvalidMat = mat;
    }

    private void Update()
    {
        HandleInput();
        HandleDeconstruction();

        if (m_isBuilding && m_currentGhost != null)
        {
            UpdateGhostPosition();
            HandleBuildingClick();
            HandleRotation();
        }
    }

    private void HandleInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.digit3Key.wasPressedThisFrame) SelectBlueprint(floorPrefab);
        if (kb.digit4Key.wasPressedThisFrame) SelectBlueprint(wallPrefab);
        if (kb.digit5Key.wasPressedThisFrame) SelectBlueprint(stoneWallPrefab);
        if (kb.digit6Key.wasPressedThisFrame) SelectBlueprint(workbenchPrefab);
        
        // B Key Toggle? "B to Open Menu" - User Request. 
        // For now, let's make B cancel building or toggle a helper UI if it exists.
        // User said: "B 눌렀을 때 건설 메뉴..." -> I will Log "Open Build Menu" for now.
        if (kb.bKey.wasPressedThisFrame) 
        {
             Debug.Log("Build Menu Toggle (UI Not Implemented yet, use 3-6 keys)");
             // Optionally Toggle Build Mode (Off)
             if (m_isBuilding) CancelBuilding();
        }

        if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) CancelBuilding();
    }

    public void SelectBlueprint(GameObject prefab)
    {
        if (prefab == null) return;

        m_currentPrefab = prefab;
        m_isBuilding = true;
        
        if (m_currentGhost) Destroy(m_currentGhost);
        
        m_currentGhost = Instantiate(prefab);
        
        // Disable Colliders
        foreach (var c in m_currentGhost.GetComponentsInChildren<Collider>()) c.enabled = false;
        
        // Set Material
        SetGhostMaterial(true);
    }

    private void CancelBuilding()
    {
        m_isBuilding = false;
        m_currentPrefab = null;
        if (m_currentGhost) Destroy(m_currentGhost);
    }

    private void HandleRotation()
    {
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        
        float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            m_rotationY += Mathf.Sign(scroll) * 45f;
        }
        m_currentGhost.transform.rotation = Quaternion.Euler(0, m_rotationY, 0);
    }

    private void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayers))
        {
            Vector3 targetPos = hit.point;

            // Snap Logic
            SnapPoint closestSnap = FindClosestSnapPoint(hit.point);
            if (closestSnap != null)
            {
                targetPos = closestSnap.transform.position;
            }

            m_currentGhost.transform.position = targetPos;

            // Validate Cost
            (string reqItem, int reqAmount) = GetBuildCost(m_currentPrefab);

            bool affordable = InventoryManager.Instance != null && InventoryManager.Instance.HasItem(reqItem, reqAmount);
            
            // Validate Workbench
            bool hasWorkbench = CheckWorkbenchRequirement();
            if (!hasWorkbench && warningText) warningText.text = "Requires Workbench!";
            else if (warningText) warningText.text = "";

            SetGhostMaterial(affordable && hasWorkbench);
        }
        else
        {
            // Floating in void
            m_currentGhost.transform.position = ray.GetPoint(maxBuildDistance);
            SetGhostMaterial(false);
            if (warningText) warningText.text = "";
        }
    }

    private bool CheckWorkbenchRequirement()
    {
        // Exception: Fireplace or Workbench itself (if added later)
        if (m_currentPrefab != null && (m_currentPrefab.name.Contains("Fireplace") || m_currentPrefab == workbenchPrefab)) return true;

        // Check for Workbench nearby
        // Optimization: Could cache closest workbench, but for now simple OverlapSphere
        Collider[] hits = Physics.OverlapSphere(m_currentGhost.transform.position, 20f); // Max Range? Or search all workbenches?
        // User said: Workbench has InfluenceRange of 20m.
        // We should check if we are inside ANY workbench's range.
        // Better: Find objects of type Workbench.
        
        // Let's rely on OverlapSphere finding the Workbench's Collider? 
        // Or finding a trigger? Workbench.cs doesn't add a collider.
        // So OverlapSphere won't find it unless it has a collider.
        // Let's assume Workbench Prefab *has* a collider.
        
        foreach(var h in hits)
        {
             if (h.GetComponent<Workbench>() || h.GetComponentInParent<Workbench>()) return true;
        }
        
        return false;
    }

    private SnapPoint FindClosestSnapPoint(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, snapRadius);
        SnapPoint closest = null;
        float minDst = float.MaxValue;

        foreach (var col in hits)
        {
            SnapPoint sp = col.GetComponent<SnapPoint>();
            if (sp != null)
            {
                float d = Vector3.Distance(pos, sp.transform.position);
                if (d < minDst)
                {
                    minDst = d;
                    closest = sp;
                }
            }
        }
        return closest;
    }

    private void SetGhostMaterial(bool isValid)
    {
        if (m_currentGhost == null) return;
        
        Material mat = isValid ? ghostValidMat : ghostInvalidMat;
        foreach (var r in m_currentGhost.GetComponentsInChildren<Renderer>())
        {
            r.material = mat;
        }
    }

    private void HandleDeconstruction()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.xKey.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayers))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain")) return;

                Debug.Log($"Deconstructed {hit.collider.name}.");
                
                Destroy(hit.collider.gameObject);
                if (audioSource && buildSound) audioSource.PlayOneShot(buildSound, 0.5f);
            }
        }
    }

    private (string, int) GetBuildCost(GameObject prefab)
    {
        if (prefab == stoneWallPrefab) return ("Stone", stoneCost);
        if (prefab == workbenchPrefab) return ("Wood", 20); // Workbench Cost
        return ("Wood", woodCost); // Default
    }

    private void HandleBuildingClick()
    {
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            (string reqItem, int reqAmount) = GetBuildCost(m_currentPrefab);

            // Check Cost
            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(reqItem, reqAmount))
            {
                Debug.Log($"Not enough {reqItem}!");
                return; 
            }

            // Workbench Check
            if (!CheckWorkbenchRequirement())
            {
                Debug.Log("Requires Workbench!");
                if (warningText) warningText.text = "Requires Workbench!";
                return;
            }

            if (InventoryManager.Instance != null)
            {
                if (CharacterStats.Instance != null)
                {
                    if (!CharacterStats.Instance.HasEnoughStamina(15f))
                    {
                         Debug.Log("Too tired to build!");
                         return;
                    }
                    CharacterStats.Instance.UseStamina(15f);
                }

                InventoryManager.Instance.RemoveItem(reqItem, reqAmount);
                GameObject built = Instantiate(m_currentPrefab, m_currentGhost.transform.position, m_currentGhost.transform.rotation);
                
                // Polish: Audio & Animation
                if (audioSource && buildSound) audioSource.PlayOneShot(buildSound);
                
                if (playerAnimator)
                {
                    playerAnimator.SetTrigger("Attack"); 
                }

                Debug.Log("Building placed!");
            }
        }
    }
}
