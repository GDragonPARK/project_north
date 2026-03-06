using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Antigravity.Tools
{
    public class AbsoluteFixRunner
    {
        [MenuItem("Antigravity/Absolute QuickSlot Force Fix")]
        public static void RunFix()
        {
            GameObject quickSlotHUD = GameObject.Find("QuickSlotHUD");
            if (quickSlotHUD == null)
            {
                Debug.LogError("[AbsoluteFixRunner] QuickSlotHUD game object not found in scene!");
                return;
            }

            QuickSlotUI quickSlotUI = quickSlotHUD.GetComponent<QuickSlotUI>();
            if (quickSlotUI == null)
            {
                Debug.LogError("[AbsoluteFixRunner] QuickSlotUI component not found on QuickSlotHUD!");
                return;
            }

            // Force Re-allocate Arrays
            quickSlotUI.slotBackground = new Image[4];
            quickSlotUI.slotIcons = new Image[4];

            // Assign Element [0] through [3] (QuickSlot_1 to QuickSlot_4)
            for (int i = 0; i < 4; i++)
            {
                string slotName = $"QuickSlot_{i + 1}";
                Transform slotTransform = quickSlotHUD.transform.Find(slotName);
                
                if (slotTransform != null)
                {
                    // Slot Background (Image on parent)
                    quickSlotUI.slotBackground[i] = slotTransform.GetComponent<Image>();
                    
                    // Slot Icon (Children named "Icon")
                    Transform iconTransform = slotTransform.Find("Icon");
                    if (iconTransform != null)
                    {
                        quickSlotUI.slotIcons[i] = iconTransform.GetComponent<Image>();
                    }
                    else
                    {
                        Debug.LogWarning($"[AbsoluteFixRunner] 'Icon' child not found for {slotName}");
                    }
                }
                else
                {
                    Debug.LogError($"[AbsoluteFixRunner] {slotName} not found under QuickSlotHUD!");
                }
            }

            EditorUtility.SetDirty(quickSlotUI);
            AssetDatabase.SaveAssets();
            Debug.Log("[AbsoluteFixRunner] QUICK SLOT UI ARRAYS FORCED! Element 0 is QuickSlot_1.");
        }
    }
}
