using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class InventoryVisualUpdate : EditorWindow
{
    [MenuItem("Antigravity/Update Inventory Visuals")]
    public static void UpdateVisuals()
    {
        string prefabPath = "Assets/valheim_Data/Prefabs/Inventory_Slot.prefab";
        string spritePath = "Assets/99.ThirdParty/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/item_slot.png";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath}");
            // Try searching
            string[] guids = AssetDatabase.FindAssets("Inventory_Slot t:Prefab");
            if (guids.Length > 0)
            {
                prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Debug.Log($"Found prefab at {prefabPath}");
            }
            else
            {
                return;
            }
        }

        Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (newSprite == null)
        {
            Debug.LogError($"Sprite not found at {spritePath}");
             // Try searching
            string[] guids = AssetDatabase.FindAssets("item_slot t:Sprite");
            if (guids.Length > 0)
            {
                spritePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                Debug.Log($"Found sprite at {spritePath}");
            }
            else
            {
                return;
            }
        }

        // Edit Prefab
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editingScope.prefabContentsRoot;
            Image bgImage = root.GetComponent<Image>();
            if (bgImage == null) bgImage = root.AddComponent<Image>();

            bgImage.sprite = newSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;
            
            // Adjust rect transform if needed (optional)
            // RectTransform rect = root.GetComponent<RectTransform>();
            // rect.sizeDelta = new Vector2(64, 64); // Example size
            
            Debug.Log("Updated Inventory_Slot prefab visuals!");
        }


        // Apply to QuickSlotUI in the scene
        QuickSlotUI quickSlotUI = GameObject.FindObjectOfType<QuickSlotUI>();
        if (quickSlotUI != null)
        {
            if (quickSlotUI.slotBackground != null)
            {
                foreach (var bg in quickSlotUI.slotBackground)
                {
                    if (bg != null)
                    {
                        Undo.RecordObject(bg, "Update QuickSlot Background");
                        bg.sprite = newSprite;
                        bg.type = Image.Type.Sliced;
                        bg.color = Color.white;
                    }
                }
                Debug.Log("Updated QuickSlotUI visuals in the scene!");
            }
        }
        else
        {
            Debug.LogWarning("QuickSlotUI not found in scene.");
        }
    }
}
