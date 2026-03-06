using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class FixIconsRunner
    {
        [MenuItem("Antigravity/Fix QuickSlot Icons and Labels")]
        public static void RunFix()
        {
            GameObject hud = GameObject.Find("QuickSlotHUD");
            if (hud == null)
            {
                Debug.LogError("[FixIconsRunner] QuickSlotHUD not found!");
                return;
            }

            ProcessSlot(hud, "QuickSlot_1", new string[] { "axe", "hatchet", "tool" }, "AXE");
            ProcessSlot(hud, "QuickSlot_2", new string[] { "hammer", "build", "wrench" }, "BUILD");
            ProcessSlot(hud, "QuickSlot_3", new string[] { "fire", "torch", "flame" }, "TORCH");

            AssetDatabase.SaveAssets();
            Debug.Log("[FixIconsRunner] Icons and Labels Force-Applied Successfully!");
        }

        private static void ProcessSlot(GameObject hud, string slotName, string[] keywords, string label)
        {
            Transform slot = hud.transform.Find(slotName);
            if (slot == null) return;

            // 1. Icon Setup
            Transform iconTr = slot.Find("Icon");
            if (iconTr == null)
            {
                GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(slot);
                iconTr = go.transform;
            }
            iconTr.gameObject.SetActive(true); // Ensure visible

            Image img = iconTr.GetComponent<Image>();
            if (img == null) img = iconTr.gameObject.AddComponent<Image>();
            img.color = Color.white;
            
            RectTransform rt = iconTr.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);

            // 2. Sprite Search
            Sprite sprite = null;
            foreach (var k in keywords)
            {
                string[] guids = AssetDatabase.FindAssets($"t:Sprite {k}");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) break;
                }
            }
            if (sprite != null) img.sprite = sprite;

            // 3. Label Setup
            Transform lbTr = slot.Find("NameLabel");
            if (lbTr == null)
            {
                GameObject go = new GameObject("NameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(slot);
                lbTr = go.transform;
            }
            lbTr.gameObject.SetActive(true);

            TextMeshProUGUI tmp = lbTr.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = lbTr.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Bottom;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;

            RectTransform lbRt = lbTr.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 0);
            lbRt.anchorMax = new Vector2(1, 0.4f);
            lbRt.offsetMin = Vector2.zero;
            lbRt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(slot.gameObject);
        }
    }
}
