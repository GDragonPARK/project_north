using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForceInteractionSetup : MonoBehaviour
{
    [Header("Configuration")]
    public int targetLayer = 10; // Item Layer
    
    private IEnumerator Start()
    {
        // 1. Wait for procedural generation (Mesh)
        yield return null; // 1 frame wait is usually enough
        
        RebuildInteractionStructure();
    }

    private void LateUpdate()
    {
        // Billboard Visuals
        Transform indicator = transform.Find("SparklePoint");
        if (indicator != null && Camera.main != null)
        {
            // Position: Fixed offset from root (local or global, user said "transform.position + Vector3.up * 1.0f")
            // Since it is a child, localPosition is easier if parent is moving, but user specified global calculation logic in request.
            // "rotation LookRotation(camera.forward)"
            
            // 1. Rotation (Billboard)
            indicator.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

            // 2. Position (Optional: if we want it strictly up regardless of log rotation)
            // If log rolls, local Y changes direction. Global Y up is better for visibility.
            indicator.position = transform.position + Vector3.up * 0.8f;
        }
    }

    public void RebuildInteractionStructure()
    {
        // --- A. DATA INJECTION ---
        ItemObject io = GetComponent<ItemObject>();
        if (io == null) io = gameObject.AddComponent<ItemObject>();
        
        if (io.itemData == null)
        {
            // 1. Try Resources
            io.itemData = Resources.Load<ItemData>("Items/Wood");
            
#if UNITY_EDITOR
            // 2. Editor Fallback (Strong Search)
            if (io.itemData == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Wood t:ItemData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    io.itemData = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    Debug.Log($"[ForceInteraction] Found Wood data via AssetDatabase: {path}");
                }
            }
#endif
            if (io.itemData != null) io.itemName = io.itemData.itemName;
        }

        // --- B. CLEAN UP (TRASH REMOVAL) ---
        // Destroy old visual holders
        Transform oldFx = transform.Find("InteractionFX");
        if (oldFx != null) DestroyImmediate(oldFx.gameObject);
        
        Transform oldSphere = transform.Find("SparkleSphere");
        if (oldSphere != null) DestroyImmediate(oldSphere.gameObject);
        
        Transform oldIndicator = transform.Find("InteractionIndicator");
        if (oldIndicator != null) DestroyImmediate(oldIndicator.gameObject);
        
        Transform oldPoint = transform.Find("SparklePoint");
        if (oldPoint != null) DestroyImmediate(oldPoint.gameObject);

        // Remove ALL child colliders (Except Root)
        Collider[] childCols = GetComponentsInChildren<Collider>();
        foreach (var c in childCols)
        {
            if (c.gameObject != gameObject)
            {
                DestroyImmediate(c); 
            }
        }

        // --- C. VISUAL INDICATOR (QUAD + UNLIT) ---
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "SparklePoint";
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.up * 0.8f; // Float above log
        indicator.transform.localScale = Vector3.one * 0.2f; // Small dot
        indicator.layer = 2; // Ignore Raycast
        
        // Remove collider from Quad
        DestroyImmediate(indicator.GetComponent<Collider>());
        
        // Material: Unlit Yellow to avoid Magenta/Lighting issues
        Renderer r = indicator.GetComponent<Renderer>();
        if (r != null)
        {
            Material glimmer = new Material(Shader.Find("Unlit/Color"));
            glimmer.color = new Color(1f, 1f, 0f, 1f); // Yellow
            r.material = glimmer;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // --- D. PHYSICS & LAYER ---
        // Force Layer 10 (Item) on Self and Children (Except Indicator)
        gameObject.layer = targetLayer;
        foreach (Transform child in transform)
        {
            if (child.gameObject == indicator) continue;
            child.gameObject.layer = targetLayer;
        }

        // Re-add BoxCollider cleanly
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) DestroyImmediate(box);
        box = gameObject.AddComponent<BoxCollider>();
        
        // Encapsulate Bounds
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer meshR in GetComponentsInChildren<Renderer>())
        {
            if (meshR.gameObject == indicator) continue; // Skip indicator
            if (meshR is SpriteRenderer) continue;

            if (!hasBounds)
            {
                bounds = meshR.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(meshR.bounds);
            }
        }

        if (hasBounds)
        {
            // Convert world bounds to local
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
            box.isTrigger = false; // Physics interaction
        }
        else
        {
            // Fallback size
            box.center = Vector3.up * 0.25f;
            box.size = Vector3.one * 0.5f;
        }
    }
}
