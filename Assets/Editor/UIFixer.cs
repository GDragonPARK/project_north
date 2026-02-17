using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class UIFixer : EditorWindow
{
    [MenuItem("Antigravity/🔧 Fix UI Layout")]
    public static void FixUI()
    {
        FixQuickSlot();
        CreateSlotPrefab();
        SetupInventoryGrid();
        FixInteractionUI();
        
        Debug.Log("<color=green><b>[UIFixer]</b> UI Layout Fixed Successfully!</color>");
    }

    static void FixQuickSlot()
    {
        QuickSlotUI qs = FindObjectOfType<QuickSlotUI>();
        if (qs != null)
        {
            GameObject go = qs.gameObject;
            RectTransform rt = go.GetComponent<RectTransform>();
            
            if (rt == null)
            {
                Debug.LogWarning($"[UIFixer] {go.name} missing RectTransform. Attempting to add one.");
                rt = go.AddComponent<RectTransform>();
            }

            if (rt != null)
            {
                Undo.RecordObject(rt, "Fix QuickSlot Pos");
                
                // Top-Left Anchor
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(50, -50); 
                
                Debug.Log($"<color=cyan>[UIFixer]</color> Fixed QuickSlotUI Position on {go.name}");
            }
        }
        else
        {
            Debug.LogWarning("[UIFixer] QuickSlotUI component not found in the scene.");
        }
    }

    static void CreateSlotPrefab()
    {
        string folder = "Assets/Resources/UI";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "UI");

        string path = folder + "/Slot_Prefab.prefab";
        
        // Create Template
        GameObject go = new GameObject("Slot_Prefab", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);
        
        // Background
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark BG

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(go.transform);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(5, 5); // Padding
        iconRT.offsetMax = new Vector2(-5, -5);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.color = Color.clear; // Invisible until set

        // Amount Text
        GameObject textObj = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0);
        textRT.anchorMax = new Vector2(1, 0.4f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.color = Color.white;
        tmp.text = "";

        // Component
        InventorySlot slot = go.AddComponent<InventorySlot>();
        slot.itemIcon = iconImg;
        slot.itemAmountText = tmp;
        // removing name text ref as requested

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        
        Debug.Log("Created/Updated Slot_Prefab at " + path);
    }

    static void SetupInventoryGrid()
    {
        InventoryUI invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null)
        {
            // Assign Prefab
            invUI.m_slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Slot_Prefab.prefab");
            
            // Setup Grid Parent
            if (invUI.m_gridParent != null)
            {
                GridLayoutGroup grid = invUI.m_gridParent.GetComponent<GridLayoutGroup>();
                if (grid == null) grid = invUI.m_gridParent.gameObject.AddComponent<GridLayoutGroup>();
                
                grid.cellSize = new Vector2(64, 64);
                grid.spacing = new Vector2(5, 5);
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
            }
            
            EditorUtility.SetDirty(invUI);
            Debug.Log("InventoryUI Configured.");
        }
    }

    static void FixInteractionUI()
    {
        InteractionUI ui = FindObjectOfType<InteractionUI>();
        if (ui != null)
        {
            // Ensure Center
            RectTransform rt = ui.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            
            // Ensure Text exists
            if (ui.interactionText == null) ui.interactionText = ui.GetComponentInChildren<TextMeshProUGUI>();
            EditorUtility.SetDirty(ui);
        }
    }
}
