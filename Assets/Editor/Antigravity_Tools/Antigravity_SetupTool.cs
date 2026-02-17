using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Antigravity_SetupTool : EditorWindow
{
    [MenuItem("Antigravity/Setup Environment References")]
    public static void SetupEnvironment()
    {
        GameObject env = GameObject.Find("Environment");
        if (env == null)
        {
            Debug.LogError("Environment GameObject not found!");
            return;
        }

        // 1. HARD CLEANUP: Remove ALL existing components
        // We use a while loop to ensure all instances are gone.
        // Note: DestroyImmediate on a required component might fail if the depender is still there.
        // So we must destroy depender first.
        
        // TerrainGenerator requires Terrain & TerrainCollider. 
        // VegetationSpawner has no requirements.
        
        // Order: Remove Scripts first, then Re-add.
        int safeCounter = 0;
        while (env.GetComponent<TerrainGenerator>() != null && safeCounter < 10)
        {
            DestroyImmediate(env.GetComponent<TerrainGenerator>());
            safeCounter++;
        }
        
        safeCounter = 0;
        while (env.GetComponent<VegetationSpawner>() != null && safeCounter < 10)
        {
            DestroyImmediate(env.GetComponent<VegetationSpawner>());
            safeCounter++;
        }
        
        Debug.Log("[Setup] All old components removed.");
        Debug.Log("[Setup] v2.0 - Starting Hard Fix...");

        // 2. Add Fresh Components
        TerrainGenerator tg = env.AddComponent<TerrainGenerator>();
        VegetationSpawner vs = env.AddComponent<VegetationSpawner>();

        // 3. Setup TerrainGenerator
        SetupTerrainGenerator(tg);
        
        // 4. Setup VegetationSpawner
        SetupVegetationSpawner(vs);
        
        // 5. Generate World Trigger
        tg.GenerateTerrain();

        Debug.Log("Antigravity: Environment Setup & Generation Complete!");
    }

    private static void SetupTerrainGenerator(TerrainGenerator tg)
    {
        // Prefabs
        // User requested: "Stone_Slab", "Gras_01" (or 02/03)
        tg.rockPrefab = FindGhibliAsset<GameObject>("Stone_Slab");
        if (tg.rockPrefab == null) tg.rockPrefab = FindGhibliAsset<GameObject>("t:Prefab Rock_01"); // Fallback
        
        tg.grassPrefab = FindGhibliAsset<GameObject>("Gras_01");
        
        // Textures
        // User requested: "Handpainted_Ground_Grass" -> Not found. Using 'Grass_ground_Base_Color'
        tg.grassTexture = FindGhibliAsset<Texture2D>("Grass_ground_Base_Color");

        tg.rockTexture = FindGhibliAsset<Texture2D>("t:Texture2D Rock_Layer"); 
        if (tg.rockTexture == null) tg.rockTexture = FindGhibliAsset<Texture2D>("Rock_001_Base_Color");

        // Force High Density for Valheim Feel
        tg.objectDensity = 30000; // Optimization: 30k
        tg.amplitude = 12f; // Slight increase for hills
        tg.frequency = 0.012f;

        EditorUtility.SetDirty(tg);
        Debug.Log($"[TerrainGenerator] Assigned: Rock={tg.rockPrefab?.name}, Grass={tg.grassPrefab?.name}, TexG={tg.grassTexture?.name}, Density={tg.objectDensity}");
    }

    private static void SetupVegetationSpawner(VegetationSpawner vs)
    {
        // 1. Assign Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) vs.player = player.transform;
        else Debug.LogError("Player with tag 'Player' not found in scene!");
        
        // 2. Assign Layer
        vs.groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain");
        if (vs.groundLayer == 0) vs.groundLayer = LayerMask.GetMask("Default"); // Fallback

        // 3. Assign Trees (Ghibli Trees)
        // User requested: Tree_01, Tree_02, Tree_03, Tree_04
        vs.treePrefabs = new List<GameObject>();
        string[] targetTrees = { "Tree_01", "Tree_02", "Tree_03", "Tree_04" };
        
        foreach (string name in targetTrees)
        {
            GameObject tree = FindGhibliAsset<GameObject>($"t:Prefab {name}");
            if (tree != null && !vs.treePrefabs.Contains(tree)) vs.treePrefabs.Add(tree);
        }

        // 4. Assign Flowers (Flower_01 ~ Flower_13)
        // Note: Prefab names in Ghibli pack usually match "Flower_01" etc.
        vs.flowerPrefabs = new List<GameObject>();
        for (int i = 1; i <= 13; i++)
        {
            string fName = $"Flower_{i:D2}"; // Flower_01, Flower_02...
            GameObject flower = FindGhibliAsset<GameObject>($"t:Prefab {fName}");
            if (flower != null && !vs.flowerPrefabs.Contains(flower)) vs.flowerPrefabs.Add(flower);
        }
        
        // 5. Assign Shrubs (Shrubs_01 ~ Shrubs_03)
        // User wrote "Shrubs_01". File search confirmed "Shrubs_01.prefab".
        vs.shrubPrefabs = new List<GameObject>();
        for (int i = 1; i <= 3; i++)
        {
            string sName = $"Shrubs_{i:D2}"; // Shrubs_01, ...
            GameObject shrub = FindGhibliAsset<GameObject>($"t:Prefab {sName}");
            if (shrub != null && !vs.shrubPrefabs.Contains(shrub)) vs.shrubPrefabs.Add(shrub);
        }

        EditorUtility.SetDirty(vs);
        Debug.Log($"[VegetationSpawner] Setup Complete. Trees:{vs.treePrefabs.Count}, Flowers:{vs.flowerPrefabs.Count}, Shrubs:{vs.shrubPrefabs.Count}");
    }

    private static T FindGhibliAsset<T>(string filter) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("GHIBLI") || path.Contains("Ghibli"))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }
        
        // Fallback
        if (guids.Length > 0)
        {
             string path = AssetDatabase.GUIDToAssetPath(guids[0]);
             return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return null;
    }

    [MenuItem("Antigravity/Fix Performance Only (Materials & Terrain)")]
    public static void OptimizeMaterialsAndTerrain()
    {
        // 1. Optimize Vegetation (GPU Instancing + LOD Culling)
        OptimizePrefabs();

        // 2. Reduce Density & Increase Scale
        GameObject env = GameObject.Find("Environment");
        if (env)
        {
            TerrainGenerator tg = env.GetComponent<TerrainGenerator>();
            VegetationSpawner vs = env.GetComponent<VegetationSpawner>();
            
            if (tg)
            {
                tg.objectDensity = 25000; // Optimal 25k
                EditorUtility.SetDirty(tg);
            }
            if (vs)
            {
                vs.minScale = 0.8f;
                vs.maxScale = 2.5f; 
                EditorUtility.SetDirty(vs);
            }
        }

        Debug.Log("[Antigravity] Optimization Complete (Materials & Terrain Updated). Hierarchy Untouched.");
    }

    private static void OptimizePrefabs()
    {
        // Find all Ghibli Prefabs used (Trees, Flowers, Shrubs)
        // We can find them via VegetationSpawner if available, or search assets
        VegetationSpawner vs = Object.FindFirstObjectByType<VegetationSpawner>();
        if (vs == null) return;

        List<GameObject> allVeg = new List<GameObject>();
        allVeg.AddRange(vs.treePrefabs);
        allVeg.AddRange(vs.flowerPrefabs);
        allVeg.AddRange(vs.shrubPrefabs);

        int optimizedCount = 0;
        foreach (GameObject prefab in allVeg)
        {
            if (prefab == null) continue;
            
            // A. GPU Instancing on Materials
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && !m.enableInstancing)
                    {
                        m.enableInstancing = true; 
                        EditorUtility.SetDirty(m);
                    }
                }
            }

            // B. LOD Group (Culled) - Checking only, avoiding structure changes if strict
            // User requested "LOD Check" previously. But "Do not touch hierarchy structure".
            // Adding a component to a PREFAB asset is safe(ish), but let's stick to "Materials & Terrain" as primary.
            // But user said "LOD 점검" (Check LOD) in the prompt before this one.
            // And in this prompt "Only Terrain and Material settings". 
            // I will COMMENT OUT LOD creation to be perfectly safe and compliant with "Only Terrain and Material".
            /*
            LODGroup lod = prefab.GetComponent<LODGroup>();
            if (lod == null)
            {
                // Component addition disabled for safety compliance
            }
            */
            
            optimizedCount++;
        }
        Debug.Log($"[Optimization] {optimizedCount} prefabs checked for GPU Instancing.");
    }
}
