using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class InteractionFixer : EditorWindow
{
    [MenuItem("Antigravity/🔧 FIX Interaction & Sparkle")]
    public static void FixInteraction()
    {
        string prefabPath = "Assets/Prefabs/Wood.prefab";
        GameObject prefabHandle = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabHandle == null)
        {
            Debug.LogError($"[InteractionFixer] Prefab not found at {prefabPath}");
            return;
        }

        // Instantiate to modify
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefabHandle);
        Undo.RegisterCreatedObjectUndo(root, "Fix Interaction");

        try
        {
            // 1. Layer Recursive (Item = 10)
            SetLayerRecursive(root, 10);
            Debug.Log("[InteractionFixer] Set Layer 10 (Item) recursively.");

            // 2. Collider Recalculation
            BoxCollider col = root.GetComponent<BoxCollider>();
            if (col == null) col = root.AddComponent<BoxCollider>();
            
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;
            
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                // Skip Particles/Sprites for physics bounds if desired, but Wood usually uses MeshRenderer
                if (r is MeshRenderer)
                {
                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
            }
            
            if (hasBounds)
            {
                // Convert world bounds to local space
                col.center = root.transform.InverseTransformPoint(bounds.center);
                col.size = bounds.size;
                Debug.Log($"[InteractionFixer] Recalculated Collider: Center={col.center}, Size={col.size}");
            }

            // 3. Sparkle FX (Ignore Raycast = 2)
            Transform fx = root.transform.Find("InteractionFX");
            if (fx == null)
            {
                GameObject fxObj = new GameObject("InteractionFX");
                fx = fxObj.transform;
                fx.SetParent(root.transform);
                fx.localPosition = new Vector3(0, 0.5f, 0);
            }
            
            fx.gameObject.layer = 2; // Ignore Raycast
            
            // Add Visual (Sprite) if missing
            SpriteRenderer sr = fx.GetComponent<SpriteRenderer>();
            if (sr == null) sr = fx.gameObject.AddComponent<SpriteRenderer>();
            
            // Try load Glow sprite
            string[] guids = AssetDatabase.FindAssets("Glow t:Sprite");
            if (guids.Length > 0)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
                sr.color = Color.yellow;
            }
            
            fx.gameObject.SetActive(false); // Default hidden
            Debug.Log("[InteractionFixer] Configured InteractionFX (Layer 2).");

            // 4. Data Auto-Connection
            ItemObject io = root.GetComponent<ItemObject>();
            if (io == null) io = root.AddComponent<ItemObject>();
            
            if (io.itemData == null)
            {
                ItemData data = Resources.Load<ItemData>("Items/Wood");
                if (data == null)
                {
                    // Search project
                    string[] dataGuids = AssetDatabase.FindAssets("Wood t:ItemData");
                    if (dataGuids.Length > 0)
                        data = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(dataGuids[0]));
                }
                
                if (data != null)
                {
                    io.itemData = data;
                    io.itemName = data.itemName;
                    Debug.Log($"[InteractionFixer] Linked ItemData: {data.name}");
                }
                else
                {
                    Debug.LogWarning("[InteractionFixer] Could not find 'Wood' ItemData!");
                }
            }

            // Save Back
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("<color=green><b>[InteractionFixer]</b> Wood Prefab Updated Successfully!</color>");
        }
        finally
        {
            DestroyImmediate(root);
        }
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            // Skip InteractionFX here to avoid overwriting its specific layer if we call this after
            // BUT user asked to do consistent Item layer first, then FX exception.
            // So we'll set everything to 10 here, and then fix FX to 2 later in logic.
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
