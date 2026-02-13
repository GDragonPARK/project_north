using UnityEngine;
using UnityEditor;

public class InventoryLayoutFixer : EditorWindow
{
    [MenuItem("Antigravity/Fix Inventory Layout")]
    public static void FixLayout()
    {
        GameObject inventoryPanel = GameObject.Find("Inventory_Panel");
        if (inventoryPanel != null)
        {
            RectTransform rect = inventoryPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                Undo.RecordObject(rect, "Fix Inventory Layout");
                
                // Set Anchor to Top-Left (0, 1)
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                
                // Reset position to nicely sit in top left (with some padding)
                rect.anchoredPosition = new Vector2(50, -50);
                
                Debug.Log("Inventory_Panel moved to Top-Left.");
            }
        }
        else
        {
            Debug.LogError("Inventory_Panel not found!");
        }

        // Check QuickSlotHUD parent
        GameObject quickSlot = GameObject.Find("QuickSlotHUD");
        GameObject inventoryCanvas = GameObject.Find("Inventory Canvas");
        
        if (quickSlot != null && inventoryCanvas != null)
        {
            if (quickSlot.transform.parent != inventoryCanvas.transform)
            {
                Undo.SetTransformParent(quickSlot.transform, inventoryCanvas.transform, "Move QuickSlotHUD");
                Debug.Log("QuickSlotHUD reparented to Inventory Canvas root.");
            }
            
            // Set QuickSlot alignment to Bottom-Center
            RectTransform qsRect = quickSlot.GetComponent<RectTransform>();
            if (qsRect != null)
            {
                Undo.RecordObject(qsRect, "Fix QuickSlot Layout");
                qsRect.anchorMin = new Vector2(0.5f, 0);
                qsRect.anchorMax = new Vector2(0.5f, 0);
                qsRect.pivot = new Vector2(0.5f, 0);
                qsRect.anchoredPosition = new Vector2(0, 50); // Padding from bottom
                Debug.Log("QuickSlotHUD aligned to Bottom-Center.");
            }
        }
    }
}
