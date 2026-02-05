using UnityEngine;
using UnityEditor;
using TMPro;

public class ManualInventorySetup : MonoBehaviour
{
    [MenuItem("Tools/Manually Setup Inventory")]
    public static void ManualSetup()
    {
        // Add Managers
        GameObject managers = GameObject.Find("GameManagers");
        if (managers == null) managers = new GameObject("GameManagers");
        if (managers.GetComponent<InventoryManager>() == null) managers.AddComponent<InventoryManager>();

        // Find or Create Canvas
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Inventory Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Add UI Root to Canvas
        GameObject panelObj = new GameObject("Inventory_Panel");
        panelObj.transform.SetParent(canvas.transform);
        UnityEngine.UI.Image img = panelObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 100);
        rect.anchoredPosition = new Vector2(0, 50);
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);

        GameObject textObj = new GameObject("ItemText");
        textObj.transform.SetParent(panelObj.transform);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Wood: 0\nStone: 0";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        
        InventoryUI ui = canvas.gameObject.AddComponent<InventoryUI>();
        ui.inventoryText = tmp;

        Debug.Log("Manual Inventory Setup Finished!");
    }
}
