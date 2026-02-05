using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnvironmentOverhaulSetup : EditorWindow
{
    [MenuItem("Tools/Final Environment Overhaul")]
    public static void Run()
    {
        // 1. Terrain Setup
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Terrain")) AssetDatabase.CreateFolder("Assets/Data", "Terrain");

            TerrainLayer grassLayer = new TerrainLayer { name = "Layer_Grass_Main" };
            AssetDatabase.CreateAsset(grassLayer, "Assets/Data/Terrain/Layer_Grass_Main.terrainlayer");
            terrain.terrainData.terrainLayers = new TerrainLayer[] { grassLayer };
        }

        // 2. Spawn 200 Grass Clusters
        VegetationSpawner spawner = GameObject.FindObjectOfType<VegetationSpawner>();
        if (spawner == null) spawner = new GameObject("FinalVegSpawner").AddComponent<VegetationSpawner>();
        spawner.grassCount = 200;
        spawner.areaSize = new Vector3(100, 0, 100);
        spawner.SpawnGrass();

        // 3. Post Processing (Comprehensive)
        GameObject ppObj = new GameObject("Overhaul Global Volume");
        Volume volume = ppObj.AddComponent<Volume>();
        volume.isGlobal = true;
        
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Overhaul_Profile";
        
        Bloom bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(2.5f);
        bloom.threshold.Override(0.9f);
        
        ColorAdjustments color = profile.Add<ColorAdjustments>();
        color.active = true;
        color.colorFilter.Override(new Color(1f, 0.88f, 0.7f)); // Warm Valheim Sunset
        color.saturation.Override(15f);
        
        Vignette vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.25f);

        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/Overhaul_Profile.asset");
        volume.profile = profile;

        // 4. Camera Settings
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.fieldOfView = 75f; // Wider FOV
            mainCam.transform.position += new Vector3(0, 2f, -5f); // Pull back and up
            mainCam.transform.LookAt(Vector3.zero);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Environment Overhaul Completed!");
    }
}
