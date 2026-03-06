using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Antigravity.Tools
{
    public class FixUILayoutRunner
    {
        [MenuItem("Antigravity/Fix UI Layout and Fill")]
        public static void RunFix()
        {
            // 1. Fix Fill Methods
            FixBarFill("StaminaBar_Fill");
            FixBarFill("HealthBar_Fill");

            // 2. Fix QuickSlotHUD Layout
            FixQuickSlotLayout();

            AssetDatabase.SaveAssets();
            Debug.Log("[FixUILayoutRunner] UI Layout and Fill fixes applied!");
        }

        private static void FixBarFill(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                Debug.LogWarning($"[FixUILayoutRunner] {objectName} not found!");
                return;
            }

            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"[FixUILayoutRunner] Image component not found on {objectName}!");
                return;
            }

            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = 0; // Bottom
            
            EditorUtility.SetDirty(img);
            Debug.Log($"[FixUILayoutRunner] {objectName} fill method set to Vertical (Bottom).");
        }

        private static void FixQuickSlotLayout()
        {
            GameObject quickSlotHUD = GameObject.Find("QuickSlotHUD");
            if (quickSlotHUD == null)
            {
                Debug.LogError("[FixUILayoutRunner] QuickSlotHUD not found!");
                return;
            }

            HorizontalLayoutGroup layout = quickSlotHUD.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = quickSlotHUD.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Reset child positions
            foreach (Transform child in quickSlotHUD.transform)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            EditorUtility.SetDirty(quickSlotHUD);
            Debug.Log("[FixUILayoutRunner] QuickSlotHUD HorizontalLayoutGroup configured and children reset.");
        }
    }
}
