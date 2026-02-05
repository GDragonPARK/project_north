using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class InventoryUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Inventory UI")]
    public static void Setup()
    {
        // 0. Cleanup existing
        GameObject existingCanvas = GameObject.Find("Inventory Canvas");
        if (existingCanvas != null)
        {
            Undo.DestroyObjectImmediate(existingCanvas);
            Debug.Log("Deleted existing Inventory Canvas.");
        }

        // 1. Create Canvas
        GameObject canvasObj = new GameObject("Inventory Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Setup Inventory UI");
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Background Panel
        GameObject panelObj = new GameObject("Inventory_Panel");
        panelObj.transform.SetParent(canvasObj.transform);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 100);
        panelRect.anchoredPosition = new Vector2(0, 50);
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);

        // 3. Create Text for Item Count
        GameObject textObj = new GameObject("ItemText");
        textObj.transform.SetParent(panelObj.transform);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Wood: 0\nStone: 0";
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(380, 80);
        textRect.anchoredPosition = Vector2.zero;

        // 3.5 Create Grid Parent for Slots
        GameObject gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(panelObj.transform);
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(50, 50);
        grid.spacing = new Vector2(5, 5);
        grid.childAlignment = TextAnchor.MiddleCenter;
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.sizeDelta = new Vector2(-20, -20);

        // 4. Attach InventoryUI Script to Canvas
        InventoryUI uiScript = canvasObj.AddComponent<InventoryUI>();
        uiScript.inventoryText = tmp;
        uiScript.m_gridParent = gridObj.transform;

        // Assign Slot Prefab
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory_Slot.prefab");
        if (slotPrefab != null)
        {
            uiScript.m_slotPrefab = slotPrefab;
            EditorUtility.SetDirty(uiScript);
            Debug.Log("Assigned Slot Prefab: " + slotPrefab.name);
        }
        else
        {
            Debug.LogError("COULD NOT FIND PREFAB AT Assets/Prefabs/Inventory_Slot.prefab!");
        }

        // 5. Ensure Managers are in Scene
        GameObject managers = GameObject.Find("GameManagers");
        if (managers == null) managers = new GameObject("GameManagers");
        
        if (managers.GetComponent<InventoryManager>() == null)
            managers.AddComponent<InventoryManager>();

        Debug.Log("Inventory UI Setup Completed!");
        Selection.activeGameObject = canvasObj;
    }
}
