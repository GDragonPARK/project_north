using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class UISystemPolisher : EditorWindow
{
    [MenuItem("Antigravity/Setup Survival UI Phase 1 (Polish Layout)")]
    public static void Setup()
    {
        // 1. Find Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (!canvas) { Debug.LogError("No Canvas Found!"); return; }

        // 2. Find Bars (Create if missing Container)
        // Group them in bottom left
        RectTransform healthBar = FindUI(canvas.transform, "HealthBar");
        RectTransform staminaBar = FindUI(canvas.transform, "StaminaBar");
        
        if (healthBar && staminaBar)
        {
            // Set Anchors to Bottom Left
            SetupBar(healthBar, new Vector2(0,0), new Vector2(0,0), new Vector2(20, 20), new Vector2(220, 50), Color.red);
            SetupBar(staminaBar, new Vector2(0,0), new Vector2(0,0), new Vector2(20, 60), new Vector2(220, 90), Color.yellow);
            // Move Stamina ABOVE Health
            
            // Wait, overlapping?
            // Health: y=20 to 50.
            // Stamina: y=60 to 90.
            // No overlap.
            
            // Adjust Texts
            SetupText(healthBar, "HP: 100");
            SetupText(staminaBar, "STAMINA: 100");
        }

        // 3. Fix Warning Text
        RectTransform warning = FindUI(canvas.transform, "WarningText");
        if (warning)
        {
            warning.anchorMin = new Vector2(0.5f, 0.5f);
            warning.anchorMax = new Vector2(0.5f, 0.5f);
            warning.anchoredPosition = new Vector2(0, 150);
            warning.sizeDelta = new Vector2(600, 100);
            var txt = warning.GetComponent<TextMeshProUGUI>();
            if (txt) { txt.fontSize = 40; txt.color = Color.red; txt.alignment = TextAlignmentOptions.Center; }
        }

        Debug.Log("UI System Polished. Bars at Bottom Left.");
    }

    private static void SetupBar(RectTransform rt, Vector2 min, Vector2 max, Vector2 posMin, Vector2 posMax, Color color)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        // rt.anchoredPosition... simpler to set sizeDelta/pos
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = posMin;
        rt.sizeDelta = posMax - posMin; // Width/Height
        
        var img = rt.GetComponent<Image>();
        if (img) img.color = color;
    }

    private static void SetupText(RectTransform parent, string defaultContent)
    {
        TextMeshProUGUI txt = parent.GetComponentInChildren<TextMeshProUGUI>();
        if (!txt)
        {
            GameObject t = new GameObject("ValueText");
            t.transform.SetParent(parent, false);
            txt = t.AddComponent<TextMeshProUGUI>();
        }
        txt.text = defaultContent;
        txt.fontSize = 20;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.black;
        
        RectTransform rt = txt.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static RectTransform FindUI(Transform root, string name)
    {
        foreach(RectTransform child in root)
        {
            if (child.name == name) return child;
            var deep = FindUI(child, name);
            if (deep) return deep;
        }
        return null;
    }
}
