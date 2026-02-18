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

    [Header("Stability Preview")]
    private float predictedStability = 0f;
    private const float SEARCH_RADIUS = 1.8f;

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
                mats[i] = validMat;
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
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain")) return;

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

    /// <summary>
    /// Calculate the predicted stability at the ghost's current position.
    /// Checks terrain contact first (grounded = 1.0), then finds best neighbor.
    /// </summary>
    public void CalculatePredictedStability()
    {
        // Check if touching terrain (would be grounded)
        Collider[] terrainHits = Physics.OverlapSphere(
            transform.position, 0.5f, LayerMask.GetMask("Terrain", "Stone", "Foundation"));

        if (terrainHits.Length > 0)
        {
            predictedStability = 1.0f;
            RefreshVisual();
            return;
        }

        // Find nearby placed BuildingPieces
        Collider[] hits = Physics.OverlapSphere(transform.position, SEARCH_RADIUS);
        float bestStability = 0f;

        foreach (var hit in hits)
        {
            // Skip self (ghost is on Ignore Raycast layer)
            if (hit.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;

            BuildingPiece piece = hit.GetComponent<BuildingPiece>();
            if (piece == null) piece = hit.GetComponentInParent<BuildingPiece>();

            if (piece != null && piece.stability > bestStability)
            {
                bestStability = piece.stability;
            }
        }

        predictedStability = Mathf.Max(0f, bestStability - BuildingPiece.DECAY_PER_STEP);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (renderers == null || renderers.Length == 0) return;

        if (isColliding)
        {
            // Collision: use invalid (red) material
            ApplyMaterialToAll(invalidMat);
        }
        else
        {
            // No collision: tint valid material with stability color
            Color stabilityColor = BuildingPiece.GetStabilityColor(predictedStability);
            stabilityColor.a = 0.5f; // Keep ghost transparency

            foreach (Renderer r in renderers)
            {
                Material[] mats = new Material[r.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = validMat;
                }
                r.materials = mats;

                // Apply tint via property block
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);

                if (validMat.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", stabilityColor);
                else
                    block.SetColor("_Color", stabilityColor);

                // Emission for night preview
                block.SetColor("_EmissionColor", stabilityColor * 0.3f);

                r.SetPropertyBlock(block);
            }
        }
    }

    private void ApplyMaterialToAll(Material mat)
    {
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            r.materials = mats;

            // Clear any property block overrides
            r.SetPropertyBlock(null);
        }
    }
}
