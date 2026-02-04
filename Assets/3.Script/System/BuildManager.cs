using UnityEngine;
using System.Collections.Generic;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("References")]
    public MyPlayerController player;
    public LayerMask buildMask;
    public Material ghostMaterial;
    
    [Header("Building Data")]
    public List<ItemData> buildablePieces; // List for UI
    public ItemData currentBuildingPiece; 
    public float buildDistance = 5f;
    public float snapRadius = 0.5f;
    [Range(0, 1)] public float refundRate = 0.5f;

    private GameObject m_ghostObject;
    public bool isBuilding { get; private set; } = false;
    private float m_currentRotation = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (player == null) return;

        // Toggle building mode (Key B)
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }

        if (isBuilding)
        {
            HandleRotation();
            UpdateGhost();
            
            // Build (LMB)
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceObject();
            }

            // Deconstruct (RMB)
            if (Input.GetMouseButtonDown(1))
            {
                TryDeconstruct();
            }
        }
    }

    public void SelectPiece(int index)
    {
        if (index >= 0 && index < buildablePieces.Count)
        {
            currentBuildingPiece = buildablePieces[index];
            if (m_ghostObject != null) Destroy(m_ghostObject);
        }
    }

    public void ToggleBuildMode()
    {
        isBuilding = !isBuilding;
        if (!isBuilding && m_ghostObject != null)
        {
            Destroy(m_ghostObject);
        }
    }

    private void HandleRotation()
    {
        if (Input.GetKey(KeyCode.E)) m_currentRotation += 150f * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) m_currentRotation -= 150f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.R)) m_currentRotation = Mathf.Round(m_currentRotation / 90f) * 90f;
    }

    private void UpdateGhost()
    {
        if (currentBuildingPiece == null || currentBuildingPiece.itemPrefab == null) return;
        
        if (m_ghostObject == null)
        {
            m_ghostObject = Instantiate(currentBuildingPiece.itemPrefab);
            SetGhostMaterial(m_ghostObject);
            DisablePhysics(m_ghostObject);
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, buildDistance, buildMask))
        {
            Vector3 targetPos = hit.point;
            Quaternion targetRot = Quaternion.Euler(0, m_currentRotation, 0);

            SnapPoint bestSnap = FindBestSnap(hit.point);
            if (bestSnap != null)
            {
                targetPos = bestSnap.transform.position;
                targetRot = bestSnap.transform.rotation * Quaternion.Euler(0, m_currentRotation, 0);
            }

            m_ghostObject.transform.position = targetPos;
            m_ghostObject.transform.rotation = targetRot;

            UpdateGhostColor();
        }
        else
        {
            m_ghostObject.transform.position = ray.origin + ray.direction * buildDistance;
        }
    }

    private void TryDeconstruct()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, buildDistance, buildMask))
        {
            StructureStability st = hit.collider.GetComponentInParent<StructureStability>();
            if (st == null) st = hit.collider.GetComponent<StructureStability>();

            if (st != null)
            {
                // Refund resources
                if (InventorySystem.Instance != null)
                {
                    ItemData data = buildablePieces.Find(p => p.itemName == st.gameObject.name);
                    int refund = data != null ? Mathf.RoundToInt(data.woodCost * refundRate) : 1;
                    InventorySystem.Instance.AddResources(refund);
                }

                Vector3 pos = st.transform.position;
                Destroy(st.gameObject);
                
                // Update nearby stability
                PropagateStabilityUpdate(pos);
                Debug.Log("<color=red>Deconstructed Piece. Resources Refunded.</color>");
            }
        }
    }

    private void UpdateGhostColor()
    {
        if (m_ghostObject == null || ghostMaterial == null) return;
        bool canAfford = InventorySystem.Instance != null && InventorySystem.Instance.HasResources(currentBuildingPiece.woodCost);
        Color color = canAfford ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        
        foreach (var rend in m_ghostObject.GetComponentsInChildren<Renderer>())
        {
            rend.material.color = color;
        }
    }

    private void SetGhostMaterial(GameObject ghost)
    {
        if (ghostMaterial == null) return;
        foreach (var rend in ghost.GetComponentsInChildren<Renderer>())
        {
            rend.material = ghostMaterial;
        }
    }

    private void DisablePhysics(GameObject ghost)
    {
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var rb in ghost.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
    }

    private SnapPoint FindBestSnap(Vector3 point)
    {
        SnapPoint[] allSnaps = Object.FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
        SnapPoint best = null;
        float minDist = snapRadius;

        foreach (var snap in allSnaps)
        {
            if (snap.transform.IsChildOf(m_ghostObject.transform)) continue;
            float dist = Vector3.Distance(point, snap.transform.position);
            if (dist < minDist) { minDist = dist; best = snap; }
        }
        return best;
    }

    private void TryPlaceObject()
    {
        if (currentBuildingPiece == null) return;
        if (InventorySystem.Instance != null)
        {
            if (InventorySystem.Instance.HasResources(currentBuildingPiece.woodCost))
            {
                InventorySystem.Instance.ConsumeResources(currentBuildingPiece.woodCost);
                PlaceObject();
            }
        }
        else PlaceObject();
    }

    private void PlaceObject()
    {
        GameObject newPiece = Instantiate(currentBuildingPiece.itemPrefab, m_ghostObject.transform.position, m_ghostObject.transform.rotation);
        newPiece.name = currentBuildingPiece.itemName;
        
        StructureStability stability = newPiece.GetComponent<StructureStability>();
        if (stability != null)
        {
            stability.UpdateStability();
            PropagateStabilityUpdate(newPiece.transform.position);
        }
    }

    private void PropagateStabilityUpdate(Vector3 position)
    {
        Collider[] nearby = Physics.OverlapSphere(position, 4f, buildMask);
        foreach (var col in nearby)
        {
            col.GetComponentInParent<StructureStability>()?.UpdateStability();
        }
    }
}
