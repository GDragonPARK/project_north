using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class StaminaUISetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Survival UI Phase 1")]
    public static void SetupUI()
    {
        // 1. Find or Create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGo = new GameObject("HUD Analysis Canvas");
            canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(cGo, "Create Canvas");
        }

        // 2. Create Panel (Bottom Left) if not exists
        Transform panel = canvas.transform.Find("StatusPanel");
        if (panel == null)
        {
            GameObject pGo = new GameObject("StatusPanel");
            pGo.transform.SetParent(canvas.transform, false);
            RectTransform rt = pGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(250, 100);
            panel = pGo.transform;
            Undo.RegisterCreatedObjectUndo(pGo, "Create StatusPanel");
        }

        // 3. Create Health Bar
        Image hpBar = SetupBar(panel, "HealthBar", Color.red, new Vector2(0, 50));
        // 4. Create Stamina Bar
        Image stBar = SetupBar(panel, "StaminaBar", Color.yellow, new Vector2(0, 0));

        // 5. Connect to CharacterStats
        CharacterStats stats = Object.FindFirstObjectByType<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("CharacterStats not found in scene!");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) 
            {
                stats = player.AddComponent<CharacterStats>();
                Debug.Log("Added CharacterStats to Player.");
            }
            else
            {
                // Create dummy
                GameObject sGo = new GameObject("CharacterStatsManager");
                stats = sGo.AddComponent<CharacterStats>();
            }
        }

        if (stats != null)
        {
            Undo.RecordObject(stats, "Bind UI to Stats");
            stats.healthBar = hpBar;
            stats.staminaBar = stBar;
            
            // Text Setup (Optional)
            // SetupText(panel, "HealthText", hpBar);
            
            EditorUtility.SetDirty(stats);
            Debug.Log("Survival UI Connected successfully!");
        }
    }

    private static Image SetupBar(Transform parent, string name, Color color, Vector2 pos)
    {
        Transform t = parent.Find(name);
        Image img = null;
        if (t == null)
        {
            // Background
            GameObject bg = new GameObject(name + "_BG");
            bg.transform.SetParent(parent, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchoredPosition = pos;
            bgRt.sizeDelta = new Vector2(200, 20);
            bgRt.anchorMin = new Vector2(0,0);
            bgRt.anchorMax = new Vector2(0,0);
            bgRt.pivot = new Vector2(0,0);

            // Foreground
            GameObject fg = new GameObject(name);
            fg.transform.SetParent(bg.transform, false);
            img = fg.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fgRt = fg.GetComponent<RectTransform>();
            fgRt.anchorMin = Vector2.zero;
            fgRt.anchorMax = Vector2.one;
            fgRt.sizeDelta = Vector2.zero; // Stretch
            
            t = fg.transform;
            Undo.RegisterCreatedObjectUndo(bg, "Create Bar");
        }
        else
        {
            img = t.GetComponent<Image>();
        }
        return img;
    }
}
