using UnityEngine;
using UnityEditor;

public class EnvironmentSetupTool : EditorWindow
{
    [MenuItem("ProjectNorth/Setup Environment")]
    public static void RunSetup()
    {
        // 1. Terrain Generator Setup
        GameObject envManager = GameObject.Find("Environment_Manager");
        if (envManager == null) envManager = new GameObject("Environment_Manager");

        TerrainGenerator generator = envManager.GetComponent<TerrainGenerator>();
        if (generator == null) generator = envManager.AddComponent<TerrainGenerator>();

        Terrain terrain = envManager.GetComponent<Terrain>();
        if (terrain == null)
        {
            terrain = envManager.AddComponent<Terrain>();
            envManager.AddComponent<TerrainCollider>();
        }

        // 2. Set Values (Smooth Hills)
        generator.amplitude = 15f; 
        generator.frequency = 0.02f;
        generator.width = 256; 
        generator.length = 256;
        generator.objectDensity = 1500; // 적당한 수

        // 3. Auto-assign Assets
        // Grass Texture (Priority: T_Grass > grass_meadows > grass)
        // User requested "T_Grass" specifically
        string[] texGuids = AssetDatabase.FindAssets("T_Grass t:Texture2D");
        if (texGuids.Length == 0) texGuids = AssetDatabase.FindAssets("grass_meadows t:Texture2D");
        if (texGuids.Length == 0) texGuids = AssetDatabase.FindAssets("grass t:Texture2D");

        if (texGuids.Length > 0)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(texGuids[0]));
            if (tex != null)
            {
                generator.grassTexture = tex;
                Debug.Log($"<color=cyan>Texture assigned: {tex.name}</color>");
            }
        }

        // Tree
        AssignPrefab(generator, ref generator.treePrefab, "PineTree", "Tree");
        // Rock
        AssignPrefab(generator, ref generator.rockPrefab, "Rock_7", "Rock");
        // Grass
        AssignPrefab(generator, ref generator.grassPrefab, "Grass_Cluster", "Grass");

        // 4. Lighting Setup
        SetupLighting();

        Debug.Log("<color=green>Environment Setup Complete! Auto-generating terrain...</color>");
        
        // 5. Auto Generate
        generator.GenerateTerrain();
        
        Selection.activeGameObject = envManager;
    }

    private static void AssignPrefab(TerrainGenerator gen, ref GameObject targetField, string specificName, string genericName)
    {
        string[] guids = AssetDatabase.FindAssets($"{specificName} t:Prefab");
        if (guids.Length == 0) guids = AssetDatabase.FindAssets($"{genericName} t:Prefab");

        if (guids.Length > 0)
        {
            targetField = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }

    private static void SetupLighting()
    {
        Light mainLight = RenderSettings.sun;
        if (mainLight == null) mainLight = Object.FindFirstObjectByType<Light>();
        
        if (mainLight != null)
        {
            mainLight.color = new Color(1f, 0.6f, 0.2f); // Sunset Orange
            mainLight.intensity = 1.5f;
            mainLight.transform.rotation = Quaternion.Euler(30, -30, 0); 
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.5f, 0.6f, 0.7f); 
        RenderSettings.fogDensity = 0.005f; 
    }
}
