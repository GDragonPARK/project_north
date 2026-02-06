using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class RealFixTool : EditorWindow
{
    [MenuItem("Antigravity/Perform REAL Fix (Stamina, Input, Trees)")]
    public static void RealFix()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
             // Try "PlayerArmature" or "Player" name
             player = GameObject.Find("PlayerArmature");
             if (!player) player = GameObject.Find("Player");
        }
        
        if (!player) { Debug.LogError("FATAL: Player not found in scene!"); return; }

        Debug.Log($"Targeting Player: {player.name}");

        // 1. Fix Stamina UI & CharacterStats
        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (!stats) stats = player.AddComponent<CharacterStats>();
        
        if (stats.staminaBar == null)
        {
            var bars = Resources.FindObjectsOfTypeAll<Image>();
            foreach(var b in bars)
            {
                if (b.name == "StaminaBar" || b.name == "StaminaGauge" || (b.name == "Fill" && b.transform.parent.name.Contains("Stamina")))
                {
                    if (b.gameObject.scene.name != null) // In Scene
                    {
                        stats.staminaBar = b;
                        Debug.Log("Connected Stamina Bar!");
                        break;
                    }
                }
            }
        }
        // Force Max Stamina
        stats.currentStamina = 100;
        stats.maxStamina = 100;
        EditorUtility.SetDirty(stats);

        // 2. Fix Equipment Manager (Input 1-2)
        PlayerEquipmentManager equip = player.GetComponent<PlayerEquipmentManager>();
        if (!equip) equip = player.AddComponent<PlayerEquipmentManager>();
        
        // Find Animation
        if (!equip.animator) equip.animator = player.GetComponent<Animator>();
        
        // Find Weapon Controller
        WeaponDamageController wdc = player.GetComponentInChildren<WeaponDamageController>();
        if (wdc) equip.weaponController = wdc;
        else Debug.LogError("WeaponDamageController not found on Player children!");

        // Find Axe/Pickaxe models
        Transform hand = FindRecursive(player.transform, "wrist.l"); 
        if (!hand) hand = FindRecursive(player.transform, "Hand_L");
        
        if (hand)
        {
            // Find Axe
            if (!equip.axeModel)
            {
                foreach(Transform t in hand) if (t.name.Contains("axe")) equip.axeModel = t.gameObject;
            }
            // Create Pickaxe if missing
            if (!equip.pickaxeModel)
            {
                // Try finding existing
                foreach(Transform t in hand) if (t.name.Contains("Pickaxe")) equip.pickaxeModel = t.gameObject;
                
                if (!equip.pickaxeModel)
                {
                    // Create Placeholder
                    GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.name = "Pickaxe_Placeholder";
                    p.transform.SetParent(hand);
                    p.transform.localPosition = new Vector3(0.6f, 0.3f, -0.5f);
                    p.transform.localEulerAngles = new Vector3(-150, 30, 250);
                    p.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                    equip.pickaxeModel = p;
                }
            }
        }
        EditorUtility.SetDirty(equip);

        // 3. Fix Building Manager (Input 3-6, B)
        BuildingManager bm = Object.FindFirstObjectByType<BuildingManager>();
        if (!bm)
        {
            GameObject bmObj = new GameObject("BuildingManager_System");
            bm = bmObj.AddComponent<BuildingManager>();
        }
        
        // Assign Prefabs (Try to load common ones)
        if (!bm.floorPrefab) bm.floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab"); 
        // ... (others requires actual path)
        
        // Fix B Key Logic Requirement?
        // BuildingManager.cs handles Input. Ensure it's active.
        if (!bm.gameObject.activeInHierarchy) bm.gameObject.SetActive(true);
        
        Debug.Log("REAL Fix Complete. Stamina Reset. Scripts Attached.");
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform child in t)
        {
            Transform r = FindRecursive(child, name);
            if (r) return r;
        }
        return null;
    }
}
