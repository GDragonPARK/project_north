using UnityEngine;
using UnityEditor;

public class UIAutoFixer : EditorWindow
{
    [MenuItem("Tools/CRITICAL FIX - UI Errors")]
    public static void FixUI()
    {
        // 1. Remove InventoryUI from Stats Canvas (it doesn't belong there)
        GameObject statsCanvas = GameObject.Find("Stats Canvas");
        if (statsCanvas != null)
        {
            InventoryUI[] ui = statsCanvas.GetComponents<InventoryUI>();
            foreach (var c in ui) 
            {
                Undo.DestroyObjectImmediate(c);
                Debug.Log("Removed misplaced InventoryUI from Stats Canvas.");
            }
        }

        // 2. Find the correct Inventory Canvas
        GameObject invCanvas = GameObject.Find("Inventory Canvas");
        if (invCanvas == null)
        {
            Debug.LogError("Inventory Canvas not found! Running Setup tool...");
            InventoryUISetup.Setup();
            invCanvas = GameObject.Find("Inventory Canvas");
        }

        if (invCanvas != null)
        {
            InventoryUI ui = invCanvas.GetComponent<InventoryUI>();
            if (ui != null)
            {
                // Check assignments
                if (ui.m_slotPrefab == null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory_Slot.prefab");
                    if (prefab != null)
                    {
                        ui.m_slotPrefab = prefab;
                        Debug.Log("Fixed missing Slot Prefab on Inventory UI.");
                    }
                }

                if (ui.m_gridParent == null)
                {
                    Transform grid = invCanvas.transform.Find("Inventory_Panel/Grid");
                    if (grid != null)
                    {
                        ui.m_gridParent = grid;
                        Debug.Log("Fixed missing Grid Parent on Inventory UI.");
                    }
                }
                
                EditorUtility.SetDirty(ui);
                AssetDatabase.SaveAssets();
            }
        }

        // 3. Remove any "Unknown" missing scripts
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in all)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        Debug.Log("UI Auto-Fix completed.");
    }
}
