using UnityEngine;
using UnityEditor;

public class ResourceDiversitySetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Resource Diversity Phase 1")]
    public static void Setup()
    {
        SetupPlayerEquipment();
        SetupMineableRock();
    }

    private static void SetupPlayerEquipment()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) { Debug.LogError("Player not found!"); return; }

        PlayerEquipmentManager equip = player.GetComponent<PlayerEquipmentManager>();
        if (!equip) equip = player.AddComponent<PlayerEquipmentManager>();

        // Find Wrist
        Transform wrist = FindRecursive(player.transform, "wrist.l"); // Assuming Left like Axe
        if (!wrist) wrist = FindRecursive(player.transform, "Hand_L"); 
        if (!wrist) { Debug.LogError("Left Wrist/Hand not found!"); return; }

        // Find Axe
        Transform axe = null;
        for(int i=0; i<wrist.childCount; i++)
        {
            if (wrist.GetChild(i).name.Contains("axe") || wrist.GetChild(i).name.Contains("Axe"))
            {
                axe = wrist.GetChild(i);
                break;
            }
        }

        // Find Pickaxe Prefab
        GameObject pickaxePrefab = FindAssetByName("PickaxeStone"); // Looking for the prefab
        
        Transform pickaxe = null;
        // Check if pickaxe already exists on wrist
        for(int i=0; i<wrist.childCount; i++)
        {
            if (wrist.GetChild(i).name.Contains("Pickaxe")) 
            {
                pickaxe = wrist.GetChild(i);
                break;
            }
        }

        if (pickaxe == null && pickaxePrefab != null)
        {
            GameObject pObj = (GameObject)PrefabUtility.InstantiatePrefab(pickaxePrefab, wrist);
            pObj.name = "Pickaxe_Equipped";
            // Align with Axe if possible
            if (axe)
            {
                pObj.transform.localPosition = axe.localPosition;
                pObj.transform.localRotation = axe.localRotation;
            }
            else
            {
                pObj.transform.localPosition = new Vector3(0.6f, 0.3f, -0.5f); // Heuristic
                pObj.transform.localEulerAngles = new Vector3(-150, 30, 250);
            }
            pickaxe = pObj.transform;
            Undo.RegisterCreatedObjectUndo(pObj, "Create Pickaxe");
        }

        if (axe) equip.axeModel = axe.gameObject;
        if (pickaxe) equip.pickaxeModel = pickaxe.gameObject;
        
        // Link WeaponController
        WeaponDamageController wdc = player.GetComponentInChildren<WeaponDamageController>();
        equip.weaponController = wdc;
        equip.animator = player.GetComponent<Animator>();
        
        // Disable Pickaxe initially
        if (pickaxe) pickaxe.gameObject.SetActive(false);
        if (axe) axe.gameObject.SetActive(true);

        EditorUtility.SetDirty(player);
        Debug.Log("Equipment Setup Complete!");
    }

    private static void SetupMineableRock()
    {
        // 1. Find or Create Stone Item Data? 
        // We assume InventoryManager works with strings or we need an Item asset.
        // Let's look for "Stone" item in Resources or create one.
        // For now, prompt user logic.
        
        // 2. Create Mineable Rock Prefab
        // Use a Sphere as placeholder or find a Rock model.
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Mineable_Rock_Prototype";
        rock.transform.localScale = Vector3.one * 2f;
        rock.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.grey };
        
        ResourceObject res = rock.AddComponent<ResourceObject>();
        res.resourceType = ResourceType.Rock;
        res.health = 5;
        // res.dropItem needs assignment.
        
        // Save as Prefab
        string path = "Assets/Prefabs/Mineable_Rock.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(rock, path);
        Object.DestroyImmediate(rock);
        
        Debug.Log($"Created Mineable Rock Prototype at {path}. Please assign 'Stone' Item to it!");
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform t = FindRecursive(child, name);
            if (t) return t;
        }
        return null;
    }

    private static GameObject FindAssetByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }
}
