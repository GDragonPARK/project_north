using UnityEngine;
using UnityEditor;

public class EnvironmentFixer : EditorWindow
{
    [MenuItem("Antigravity/Fix Environment & Pink Terrain")]
    public static void Fix()
    {
        // 1. Fix Duplicates
        GameObject envMgr = GameObject.Find("Environment_Manager");
        GameObject env = GameObject.Find("Environment");

        // Prefer Environment_Manager. Move children of Environment to Manager if useful?
        if (envMgr && env && envMgr != env)
        {
            Debug.Log("Found Duplicate Environments. Merging...");
            // Check content. Usually Environment has Lighting/Volume.
            // Move Env children to EnvManager
            while(env.transform.childCount > 0)
            {
                Transform t = env.transform.GetChild(0);
                t.SetParent(envMgr.transform);
            }
            DestroyImmediate(env);
            Debug.Log("Legacy 'Environment' object deleted.");
        }

        // 2. Fix Pink Terrain
        TerrainGenerator tg = Object.FindObjectOfType<TerrainGenerator>();
        if (tg)
        {
            // Check Textures
            if (!tg.rockTexture) tg.rockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Terrain/Rock.jpg"); // Placeholder path
            if (!tg.grassTexture) tg.grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Terrain/Grass.jpg");
            
            // If still null, try finding ANY texture
            string[] guids;
            if (!tg.grassTexture) 
            {
                 guids = AssetDatabase.FindAssets("Grass t:Texture2D");
                 if (guids.Length > 0) tg.grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (!tg.rockTexture) 
            {
                 guids = AssetDatabase.FindAssets("Rock t:Texture2D");
                  if (guids.Length > 0) tg.rockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                 else if (tg.grassTexture) tg.rockTexture = tg.grassTexture; // Fallback
            }

            // Assign Material if missing?
            Terrain t = tg.GetComponent<Terrain>();
            if (t && t.materialTemplate == null)
            {
                 // Create Default Terrain Material
                 Material mat = new Material(Shader.Find("Nature/Terrain/Standard"));
                 if (!mat) mat = new Material(Shader.Find("Standard")); 
                 t.materialTemplate = mat;
            }

            Debug.Log($"Terrain Textures Fixed? Rock: {tg.rockTexture}, Grass: {tg.grassTexture}. RE-GENERATE Terrain Now.");
        }
    }
}
