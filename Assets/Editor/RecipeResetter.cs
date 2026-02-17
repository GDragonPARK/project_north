using UnityEngine;
using UnityEditor;
using System.IO;

public class RecipeResetter : EditorWindow
{
    [MenuItem("Antigravity/☢️ NUKE & RESET Building System")]
    public static void NukeAndReset()
    {
        // 1. Delete Old Assets
        string path = "Assets/Resources/Building/Recipes";
        if (Directory.Exists(path))
        {
            // Delete specific files to be safe, or just clear the folder content? 
            // User said "Delete Wood_Floor.asset and Wood_Wall.asset"
            AssetDatabase.DeleteAsset(path + "/Wood_Floor.asset");
            AssetDatabase.DeleteAsset(path + "/Wood_Wall.asset");
            AssetDatabase.Refresh();
        }
        else
        {
            Directory.CreateDirectory(path);
        }

        // 2. Find Prefabs
        GameObject floorPrefab = FindPrefab("PP_Floor_Tile_05");
        GameObject wallPrefab = FindPrefab("Wall_Prefab"); // Or 'Wood_Wall' if renamed? 
        // Note: Project might have different names, try to find broadly
        if (wallPrefab == null) wallPrefab = FindPrefab("Wood_Wall_Prefab");
        if (wallPrefab == null) wallPrefab = FindPrefab("SimpleWall");

        if (floorPrefab == null || wallPrefab == null)
        {
            if (floorPrefab == null) Debug.LogError("❌ COULD NOT FIND 'PP_Floor_Tile_05.prefab'. Check if it exists or is only an FBX.");
            if (wallPrefab == null) Debug.LogError("❌ COULD NOT FIND 'Wall_Prefab.prefab' (or variants).");
            Debug.LogError("Aborting reset due to missing prefabs.");
            return;
        }

        // 3. Create New Recipes
        CreateRecipe("Wood_Floor", floorPrefab, path);
        CreateRecipe("Wood_Wall", wallPrefab, path);

        AssetDatabase.SaveAssets();

        // 4. Reset Scene Object
        BuildingManager existing = Object.FindFirstObjectByType<BuildingManager>();
        if (existing)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject newManager = new GameObject("BuildingManager_System");
        BuildingManager bm = newManager.AddComponent<BuildingManager>();
        
        // Re-assign basic defaults if possible, or reliance on Resources.Load will handle it
        // The script uses Resources.Load("Building/Recipes/...") in TrySelectRecipeByName, so we are good.
        
        Debug.Log("<color=green>☢️ SYSTEM RESET COMPLETE. Recipes Recreated & Manager Reinstantiated.</color>");
    }

    private static void CreateRecipe(string name, GameObject prefab, string path)
    {
        BuildRecipeSO recipe = ScriptableObject.CreateInstance<BuildRecipeSO>();
        recipe.pieceName = name;
        recipe.prefab = prefab;
        recipe.category = "Basic";
        
        // Add default cost?
        recipe.costs = new System.Collections.Generic.List<BuildCost>();
        ItemData wood = Resources.Load<ItemData>("Items/Wood"); 
        if (wood)
        {
            recipe.costs.Add(new BuildCost { item = wood, amount = 2 });
        }

        AssetDatabase.CreateAsset(recipe, $"{path}/{name}.asset");
    }

    private static GameObject FindPrefab(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:GameObject");
        foreach(var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // 🛡️ Fix: Strictly filter for .prefab to avoid selecting FBX models
            if (path.Contains("Assets/") && path.EndsWith(".prefab")) 
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        return null;
    }
}
