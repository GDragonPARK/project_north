using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class ValheimUI_Builder : EditorWindow
{
    [MenuItem("Antigravity/🏗️ Rebuild Valheim UI & Interaction")]
    public static void Build()
    {
        RebuildUI();
        AttachInteractionScripts();
        SetupAutoPickup();
    }

    static void RebuildUI()
    {
        InventoryUI ui = Object.FindAnyObjectByType<InventoryUI>();
        if (ui == null)
        {
            Debug.LogError("InventoryUI not found in scene!");
            return;
        }

        GameObject panelObj = ui.inventoryPanel;
        if (panelObj == null)
        {
            Transform found = ui.transform.Find("Inventory_Panel");
            if (found) panelObj = found.gameObject;
            else panelObj = GameObject.Find("Inventory_Panel");
        }

        if (panelObj == null)
        {
            Debug.LogError("Inventory_Panel not found.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(panelObj, "Rebuild UI");

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in panelObj.transform) children.Add(child.gameObject);
        foreach (GameObject child in children) DestroyImmediate(child);

        Image bg = panelObj.GetComponent<Image>();
        if (bg == null) bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(400, 300);
        rt.anchoredPosition = new Vector2(20, -150);

        GridLayoutGroup grid = panelObj.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = panelObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(60, 60);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.childAlignment = TextAnchor.UpperLeft;

        ui.inventoryPanel = panelObj;
        ui.m_gridParent = panelObj.transform;
        
        if (ui.m_slotPrefab == null)
        {
            ui.m_slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Slot_Prefab.prefab");
        }

        EditorUtility.SetDirty(ui);
        Debug.Log("<color=orange><b>[Builder]</b> UI Rebuilt.</color>");
    }

    static void AttachInteractionScripts()
    {
        ItemObject[] items = Object.FindObjectsByType<ItemObject>(FindObjectsSortMode.None);
        int count = 0;

        foreach (ItemObject item in items)
        {
            if (item.itemName == "Wood" || item.name.Contains("Wood") || item.name.Contains("Log"))
            {
                ForceInteractionSetup setup = item.GetComponent<ForceInteractionSetup>();
                if (setup == null)
                {
                    setup = item.gameObject.AddComponent<ForceInteractionSetup>();
                    Undo.RegisterCreatedObjectUndo(setup, "Attach ForceInteraction");
                }
                setup.RebuildInteractionStructure();
                count++;
            }
        }
        Debug.Log($"<color=cyan><b>[Builder]</b> Updated ForceInteractionSetup on {count} wood items.</color>");
    }

    static void SetupAutoPickup()
    {
        // Ensure Player has the Collector Marker
        GameObject player = GameObject.Find("Player_New");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (player.GetComponent<AutoPickupCollector>() == null)
            {
                player.AddComponent<AutoPickupCollector>();
                Debug.Log("[Builder] Added AutoPickupCollector marker to Player.");
            }
        }

        ItemObject[] items = Object.FindObjectsByType<ItemObject>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var item in items)
        {
            if (item.GetComponent<PickupItem>() == null)
            {
                item.gameObject.AddComponent<PickupItem>();
                count++;
            }
        }
        Debug.Log($"<color=cyan><b>[Builder]</b> Autonomous Auto-Pickup components (Trigger-less) added to {count} items.</color>");
    }
}
