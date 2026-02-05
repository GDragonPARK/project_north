using UnityEngine;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public LayerMask layerMask; // Terrain + Buildable
    public Material ghostMaterial;
    public Material ghostInvalidMaterial;

    [Header("Settings")]
    public float rotateSpeed = 90f;
    public float reachDistance = 5f;

    private GameObject currentPrefab;
    private GameObject ghostObject;
    private bool isBuilding = false;
    private float currentRotationY = 0f;

    public void EnterBuildMode()
    {
        isBuilding = true;
    }

    public void ExitBuildMode()
    {
        isBuilding = false;
        if (ghostObject) Destroy(ghostObject);
    }

    public void SelectPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
             Debug.LogError("[PlacementController] Cannot select null prefab.");
             return;
        }

        currentPrefab = prefab;
        if (ghostObject) Destroy(ghostObject);
        
        if (currentPrefab)
        {
            ghostObject = Instantiate(currentPrefab);
            if (ghostObject == null)
            {
                 Debug.LogError("[PlacementController] Failed to instantiate ghost object.");
                 return;
            }
            
            // Remove colliders from ghost
            foreach (var c in ghostObject.GetComponentsInChildren<Collider>()) Destroy(c);
            
            // Apply ghost material
            if (ghostMaterial != null)
            {
                foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
                {
                    r.material = ghostMaterial;
                }
            }
        }
    }

    private void Update()
    {
        if (!isBuilding || ghostObject == null) return;

        HandleRotation();
        HandlePlacementPosition();
        HandleClick();
    }

    private void HandleRotation()
    {
        // Mouse Wheel to rotate
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.1f)
        {
            currentRotationY += scroll * 22.5f; // Snap to 22.5 degrees like Valheim
        }
        ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    private void HandlePlacementPosition()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Raycast against everything except Player layer if needed, or specified masks
        // Adding "Default" and "Terrain" explicitly if layerMask is not set
        int mask = layerMask.value == 0 ? LayerMask.GetMask("Default", "Terrain") : layerMask.value;

        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, mask))
        {
            Vector3 targetPos = hit.point;

            // Simple Snapping Logic (Check for SnapPoints nearby)
            Collider[] colliders = Physics.OverlapSphere(hit.point, 1f); 
            foreach (var col in colliders)
            {
                if (col.CompareTag("SnapPoint")) // Assume SnapPoints are tagged
                {
                    targetPos = col.transform.position;
                    break;
                }
            }
            
            ghostObject.transform.position = targetPos;
        }
    }

    private void HandleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Place
            Instantiate(currentPrefab, ghostObject.transform.position, ghostObject.transform.rotation);
        }
    }
}
