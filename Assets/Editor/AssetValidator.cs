using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AssetValidator : EditorWindow
{
    [MenuItem("Antigravity/🛠️ Validate Build Recipes")]
    public static void ValidateAndFixRecipes()
    {
        string[] guids = AssetDatabase.FindAssets("t:BuildRecipeSO");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildRecipeSO recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeSO>(path);
            
            if (recipe == null) continue;

            bool dirty = false;
            
            // 1. Check Main Prefab
            if (recipe.prefab == null)
            {
                Debug.LogWarning($"[AssetValidator] {recipe.name}: Prefab is missing!");
                
                // Attempt Auto-Fix based on name
                GameObject found = FindPrefabByName(recipe.pieceName) ?? FindPrefabByName(recipe.name);
                if (found != null)
                {
                    recipe.prefab = found;
                    Debug.Log($"<color=green> -> Fixed: Assigned {found.name}</color>");
                    dirty = true;
                }
            }

            // 2. Check Preview Prefab (Optional but good to check)
            if (recipe.previewPrefab == null)
            {
                // Not strictly an error if logic handles null, but let's see
                // If it's missing, maybe default to main prefab?
            }

            if (dirty)
            {
                EditorUtility.SetDirty(recipe);
                fixedCount++;
            }
        }

        if (fixedCount > 0) AssetDatabase.SaveAssets();
        Debug.Log($"[AssetValidator] Scan Complete. Fixed {fixedCount} assets.");
    }

    private static GameObject FindPrefabByName(string name)
    {
        string[] search = new string[] 
        { 
            name, 
            "PP_" + name, 
            name.Replace("Wood_", "PP_Wood_"), 
            name.Replace("Wood_", "") 
        };

        foreach (var s in search)
        {
            string[] guids = AssetDatabase.FindAssets($"{s} t:GameObject");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains("Assets/Prefabs") || path.Contains("Assets/Resources"))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
        }
        return null;
    }
}
