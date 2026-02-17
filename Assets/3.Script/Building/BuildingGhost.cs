using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingGhost : MonoBehaviour
{
    [Header("Visual State")]
    private Renderer[] renderers;
    private Material validMat;
    private Material invalidMat;
    
    [Header("Collision State")]
    public bool isColliding = false;
    private int collisionCount = 0;

    public void Setup(Material valid, Material invalid)
    {
        validMat = valid;
        invalidMat = invalid;

        // 1. Collect all renderers
        renderers = GetComponentsInChildren<Renderer>();
        
        // 2. Replace materials with ghost materials
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = validMat; // Start with valid (green)
            }
            r.materials = mats;
        }

        // 3. Set layer to Ignore Raycast to prevent self-collision
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Ignore Raycast"));

        // 4. Set all colliders to trigger mode
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.isTrigger = true;
        }

        // 5. Add Rigidbody if not present (required for trigger events)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore terrain - we can place on terrain
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain")) return;

        // Check for obstacles, default objects, or player
        string layerName = LayerMask.LayerToName(other.gameObject.layer);
        if (layerName == "Obstacle" || layerName == "Default" || layerName == "Player")
        {
            collisionCount++;
            isColliding = true;
            RefreshVisual();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Ignore terrain
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain")) return;

        string layerName = LayerMask.LayerToName(other.gameObject.layer);
        if (layerName == "Obstacle" || layerName == "Default" || layerName == "Player")
        {
            collisionCount--;
            if (collisionCount <= 0)
            {
                collisionCount = 0;
                isColliding = false;
                RefreshVisual();
            }
        }
    }

    public void RefreshVisual()
    {
        if (renderers == null || renderers.Length == 0) return;

        Material targetMat = isColliding ? invalidMat : validMat;
        
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = targetMat;
            }
            r.materials = mats;
        }
    }
}
