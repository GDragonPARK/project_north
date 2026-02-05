using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class StatsUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Stats UI")]
    public static void Setup()
    {
        // Find existing Canvas or create one
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Stats Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Stats Container
        GameObject statsRoot = new GameObject("Stats_UI");
        statsRoot.transform.SetParent(canvas.transform);
        RectTransform rootRect = statsRoot.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400, 100);
        rootRect.anchoredPosition = new Vector2(0, 50);
        rootRect.anchorMin = new Vector2(0.5f, 0);
        rootRect.anchorMax = new Vector2(0.5f, 0);

        // Health Bar
        GameObject hbBack = CreateBar(statsRoot.transform, "HealthBar", new Color(0.2f, 0, 0, 0.8f), new Color(1, 0, 0, 1), new Vector2(0, 20));
        // Stamina Bar
        GameObject sbBack = CreateBar(statsRoot.transform, "StaminaBar", new Color(0.2f, 0.2f, 0, 0.8f), new Color(1, 1, 0, 1), new Vector2(0, -20));

        // Update CharacterStats references
        CharacterStats stats = GameObject.FindFirstObjectByType<CharacterStats>();
        if (stats == null)
        {
            GameObject managers = GameObject.Find("GameManagers");
            if (managers == null) managers = new GameObject("GameManagers");
            stats = managers.AddComponent<CharacterStats>();
        }

        stats.healthBar = hbBack.transform.GetChild(0).GetComponent<Image>();
        stats.staminaBar = sbBack.transform.GetChild(0).GetComponent<Image>();
        
        Debug.Log("Stats UI Setup Completed!");
    }

    private static GameObject CreateBar(Transform parent, string name, Color bgColor, Color fillContext, Vector2 pos)
    {
        GameObject bg = new GameObject(name + "_BG");
        bg.transform.SetParent(parent);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(300, 20);
        bgRect.anchoredPosition = pos;

        GameObject fill = new GameObject(name + "_Fill");
        fill.transform.SetParent(bg.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillContext;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        return bg;
    }
}
