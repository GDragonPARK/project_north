using UnityEngine;
using UnityEditor;

public class TreeLootFixer : EditorWindow
{
    [MenuItem("Antigravity/🌲 FIX Tree Loot Data")]
    public static void FixTreeLoot()
    {
        // 1. Load Wood Prefab
        GameObject woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wood.prefab");
        if (woodPrefab == null)
        {
            // Try finding it
             string[] guids = AssetDatabase.FindAssets("Wood t:GameObject");
             foreach(var g in guids)
             {
                 string p = AssetDatabase.GUIDToAssetPath(g);
                 if (p.EndsWith("Wood.prefab")) 
                 {
                     woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                     break;
                 }
             }
        }

        if (woodPrefab == null)
        {
            Debug.LogError("❌ Critical: Could not find 'Wood.prefab'! Cannot fix trees.");
            return;
        }

        // 2. Find All Tree Prefabs (with ResourceNode)
        string[] treeGuids = AssetDatabase.FindAssets("t:GameObject");
        int fixedCount = 0;

        foreach (var guid in treeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;

            // Optimization: Skip non-relevant folders? Or just check component
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj == null) continue;

            // Check for ResourceNode
            ResourceNode node = obj.GetComponent<ResourceNode>();
            if (node != null)
            {
                // We found a tree!
                bool dirty = false;
                
                // Fix Loot Prefab
                if (node.lootPrefab == null)
                {
                    node.lootPrefab = woodPrefab;
                    dirty = true;
                    Debug.Log($"[TreeFix] Assigned Wood to {obj.name}");
                }

                // Fix Loot Amount
                if (node.lootAmount == 0)
                {
                    node.lootAmount = 3;
                    dirty = true;
                    Debug.Log($"[TreeFix] Set Amount to 3 for {obj.name}");
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(obj);
                    fixedCount++;
                }
            }
        }
        
        // 3. Force Save
        if (fixedCount > 0) AssetDatabase.SaveAssets();
        
        Debug.Log($"<color=green>✅ Tree Loot Fix Complete. Updated {fixedCount} prefabs.</color>");
        
        // 4. Update Scene Objects (Optional but good)
        ResourceNode[] sceneNodes = FindObjectsOfType<ResourceNode>();
        int sceneFixed = 0;
        foreach(var node in sceneNodes)
        {
            if (node.lootPrefab == null) { node.lootPrefab = woodPrefab; sceneFixed++; }
            if (node.lootAmount == 0) { node.lootAmount = 3; sceneFixed++; }
        }
        if (sceneFixed > 0) Debug.Log($"[TreeFix] Also updated {sceneFixed} trees in the current scene.");
    }
}
