using UnityEngine;
using UnityEditor;

public class SetupResourcesTool : EditorWindow
{
    [MenuItem("Tools/Setup Survival Resources")]
    public static void Setup()
    {
        // 1. Wood Asset Setup
        InventoryItem woodAsset = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Data/Items/Wood.asset");
        if (woodAsset != null) woodAsset.itemName = "Wood";

        // 2. Stone Asset Setup
        InventoryItem stoneAsset = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Data/Items/Stone.asset");
        if (stoneAsset != null) stoneAsset.itemName = "Stone";

        EditorUtility.SetDirty(woodAsset);
        EditorUtility.SetDirty(stoneAsset);
        AssetDatabase.SaveAssets();

        // 3. Tree Setup
        GameObject tree = GameObject.Find("Tree");
        if (tree != null)
        {
            ResourceObject res = tree.GetComponent<ResourceObject>();
            if (res != null) Undo.DestroyObjectImmediate(res);
            res = tree.AddComponent<ResourceObject>();
            res.dropItem = woodAsset;
            res.health = 3;
            Debug.Log("Tree setup completed with Wood asset.");
        }

        // 4. Rock Setup
        GameObject rock = GameObject.Find("Rock");
        if (rock != null)
        {
            ResourceObject res = rock.GetComponent<ResourceObject>();
            if (res != null) Undo.DestroyObjectImmediate(res);
            res = rock.AddComponent<ResourceObject>();
            res.dropItem = stoneAsset;
            res.health = 5;
            Debug.Log("Rock setup completed with Stone asset.");
        }

        AssetDatabase.Refresh();
        Debug.Log("Survival Resource Setup Finished!");
    }
}
