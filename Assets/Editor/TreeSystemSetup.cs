using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using StarterAssets;

public class TreeSystemSetup : EditorWindow
{
    [MenuItem("Antigravity/🌲 Setup Tree Chopping")]
    public static void SetupTreePrefabs()
    {
        // Find existing tree prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:GameObject Tree");
        GameObject woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/valheim_Data/GameElements/Items/materials/Wood.prefab");

        if (woodPrefab == null)
        {
            Debug.LogError("Wood prefab not found at Assets/valheim_Data/GameElements/Items/materials/Wood.prefab");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Filter: Only .prefab files
            if (!path.EndsWith(".prefab")) continue;
            
            // Filter: Usually trees are in /Prefabs/ folders, not /fx/ or /VFX/
            if (!path.Contains("/Prefabs/")) continue;
            if (path.ToLower().Contains("/fx/") || path.ToLower().Contains("/vfx/") || path.ToLower().Contains("/sfx/")) continue;

            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (treePrefab == null) continue;

            if (treePrefab.GetComponent<ResourceNode>() != null) continue;

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = editScope.prefabContentsRoot;
                
                ResourceNode node = root.AddComponent<ResourceNode>();
                node.maxHealth = 100f;
                node.lootPrefab = woodPrefab;
                node.lootAmount = 3;

                // Add Collider if missing
                if (root.GetComponent<Collider>() == null)
                {
                    CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
                    cap.radius = 0.5f;
                    cap.height = 5f;
                    cap.center = new Vector3(0, 2.5f, 0);
                }
            }
            Debug.Log($"Setup ResourceNode on {treePrefab.name}");
        }

        // Setup Wood Prefab with ItemObject
        string woodPath = AssetDatabase.GetAssetPath(woodPrefab);
        ItemData woodData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Wood.asset");

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(woodPath))
        {
            GameObject woodRoot = editScope.prefabContentsRoot;
            ItemObject item = woodRoot.GetComponent<ItemObject>();
            if (item == null) item = woodRoot.AddComponent<ItemObject>();
            item.itemName = "Wood";
            item.amount = 1;
            item.itemData = woodData;

            // Ensure collider for raycast
            SphereCollider sphere = woodRoot.GetComponent<SphereCollider>();
            if (sphere == null) sphere = woodRoot.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
            
            woodRoot.layer = 0; 
        }
        
        Debug.Log("✅ Tree and Wood Prefabs Setup Complete.");
    }

    [MenuItem("Antigravity/Setup Axe Physics")]
    public static void SetupAxePhysics()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;

        AxeInteraction axe = player.GetComponentInChildren<AxeInteraction>(true);
        if (axe == null)
        {
            // Try to find the axe model
            Transform axeModel = player.transform.Find("Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/Weapon_Socket/axe_1handed");
            if (axeModel)
            {
                axe = axeModel.gameObject.GetComponent<AxeInteraction>();
                if (!axe) axe = axeModel.gameObject.AddComponent<AxeInteraction>();
            }
        }

        if (axe)
        {
            Rigidbody rb = axe.GetComponent<Rigidbody>();
            if (!rb) rb = axe.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            BoxCollider col = axe.GetComponent<BoxCollider>();
            if (!col) col = axe.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            Debug.Log("✅ Axe Physics Setup Complete.");
        }
    }

    [MenuItem("Antigravity/Setup Interaction UI")]
    public static void SetupInteractionUI()
    {
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            PlayerInteraction interact = player.GetComponent<PlayerInteraction>();
            if (interact == null) interact = player.AddComponent<PlayerInteraction>();
            interact.cam = Camera.main;
            interact.interactDistance = 4f;
            interact.interactLayer = ~0; 
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        InteractionUI ui = FindObjectOfType<InteractionUI>();
        if (ui == null)
        {
            GameObject uiObj = new GameObject("InteractionHUD");
            uiObj.transform.SetParent(canvas.transform, false);
            ui = uiObj.AddComponent<InteractionUI>();

            GameObject panel = new GameObject("InteractionPanel");
            panel.transform.SetParent(uiObj.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 60);
            panelRect.anchoredPosition = new Vector2(0, -100);

            GameObject textObj = new GameObject("InteractionText");
            textObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "[E] Pick Up";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
            tmp.color = Color.yellow;
            ui.interactionPanel = panel;
            ui.interactionText = tmp;
        }
        Debug.Log("✅ Interaction UI Setup Complete.");
    }

    [MenuItem("Antigravity/Setup Inventory & UI")]
    public static void SetupInventoryUI()
    {
        // Assets paths
        string slotSpritePath = "Assets/99.ThirdParty/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/crafting_slot_01.png";
        string fontPath = "Assets/99.ThirdParty/Artsystack - Fantasy RPG GUI/ResourcesData/Font/MedievalSharp-Regular SDF.asset";
        
        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(slotSpritePath);
        TMP_FontAsset medievalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

        // 1. Setup InventorySystem (Manager)
        InventorySystem invSystem = FindObjectOfType<InventorySystem>();
        if (invSystem == null)
        {
            GameObject invObj = new GameObject("InventorySystem");
            invSystem = invObj.AddComponent<InventorySystem>();
        }

        // 2. Setup QuickSlot UI
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Main Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        QuickSlotUI quickUI = FindObjectOfType<QuickSlotUI>();
        if (quickUI == null)
        {
            GameObject quickObj = new GameObject("QuickSlotHUD");
            quickObj.transform.SetParent(canvas.transform, false);
            quickUI = quickObj.AddComponent<QuickSlotUI>();

            quickUI.slotIcons = new Image[4];
            quickUI.slotBackground = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject slot = new GameObject($"QuickSlot_{i + 1}");
                slot.transform.SetParent(quickObj.transform, false);
                RectTransform slotRect = slot.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(70, 70);
                slotRect.anchoredPosition = new Vector2(-150 + (i * 80), 60);
                slotRect.anchorMin = new Vector2(0.5f, 0);
                slotRect.anchorMax = new Vector2(0.5f, 0);

                Image bg = slot.AddComponent<Image>();
                bg.sprite = slotSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(1, 1, 1, 0.8f);
                quickUI.slotBackground[i] = bg;

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.rectTransform.sizeDelta = new Vector2(50, 50);
                icon.gameObject.SetActive(false);
                quickUI.slotIcons[i] = icon;

                // Number text
                GameObject numObj = new GameObject("Number");
                numObj.transform.SetParent(slot.transform, false);
                TextMeshProUGUI numText = numObj.AddComponent<TextMeshProUGUI>();
                numText.text = (i + 1).ToString();
                numText.fontSize = 18;
                if (medievalFont) numText.font = medievalFont;
                numText.alignment = TextAlignmentOptions.TopLeft;
                numText.color = new Color(0.18f, 0.64f, 1f); // Valheim blue-ish number
                
                RectTransform numRect = numText.rectTransform;
                numRect.anchoredPosition = new Vector2(8, -8);
                numRect.anchorMin = new Vector2(0, 1);
                numRect.anchorMax = new Vector2(0, 1);
                numRect.sizeDelta = new Vector2(30,30);
            }
        }
        else
        {
            // Update existing quickslots if they exist
            foreach (var bg in quickUI.slotBackground)
            {
                if (bg)
                {
                    bg.sprite = slotSprite;
                    bg.type = Image.Type.Sliced;
                }
            }
        }

        // 3. Update Inventory Slot Prefab Visuals
        InventoryUI invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null && invUI.m_slotPrefab != null)
        {
            string slotPath = AssetDatabase.GetAssetPath(invUI.m_slotPrefab);
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(slotPath))
            {
                GameObject slotRoot = editScope.prefabContentsRoot;
                
                // Background
                Image bgImage = slotRoot.GetComponent<Image>();
                if (bgImage)
                {
                    bgImage.sprite = slotSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = new Color(1, 1, 1, 0.8f);
                }

                InventorySlot slotComp = slotRoot.GetComponent<InventorySlot>();
                if (slotComp == null) slotComp = slotRoot.AddComponent<InventorySlot>();

                // Hierarchy check/setup
                Transform iconTrans = slotRoot.transform.Find("Icon");
                if (iconTrans) slotComp.itemIcon = iconTrans.GetComponent<Image>();

                Transform nameTrans = slotRoot.transform.Find("Count"); // Many prefabs use 'Count'
                if (nameTrans == null) nameTrans = slotRoot.transform.Find("Name");
                if (nameTrans == null) nameTrans = slotRoot.transform.Find("Text");

                if (nameTrans)
                {
                    TextMeshProUGUI tmp = nameTrans.GetComponent<TextMeshProUGUI>();
                    if (tmp)
                    {
                        slotComp.itemNameText = tmp;
                        if (medievalFont) tmp.font = medievalFont;
                        tmp.fontSize = 14;
                        tmp.alignment = TextAlignmentOptions.BottomRight;
                        tmp.color = Color.white;
                        
                        RectTransform textRect = tmp.rectTransform;
                        textRect.anchorMin = new Vector2(0.5f, 0);
                        textRect.anchorMax = new Vector2(1, 0.5f);
                        textRect.anchoredPosition = new Vector2(-5, 5);
                    }
                }
            }
        }

        Debug.Log("✅ Valheim Style UI Setup Complete.");
    }
}
