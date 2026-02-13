using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EnvironmentAutoSetup : EditorWindow
{
    [MenuItem("Antigravity/Fix Environment & Tree Variety")]
    public static void FixEnvironment()
    {
        Debug.Log("Starting Environment Auto-Setup...");

        // 1. Merge Managers
        GameObject env = GameObject.Find("Environment");
        GameObject envMgr = GameObject.Find("Environment_Manager");

        if (env == null && envMgr != null)
        {
            envMgr.name = "Environment";
            env = envMgr;
            Debug.Log("Renamed Environment_Manager to Environment.");
        }
        else if (env != null && envMgr != null)
        {
            // Move children
            while(envMgr.transform.childCount > 0)
            {
                Transform child = envMgr.transform.GetChild(0);
                child.SetParent(env.transform);
            }
            DestroyImmediate(envMgr);
            Debug.Log("Merged Environment_Manager into Environment.");
        }

        if (env == null)
        {
            Debug.LogError("No Environment object found!");
            return;
        }

        // 2. Setup TerrainGenerator
        TerrainGenerator tg = env.GetComponent<TerrainGenerator>();
        if (!tg) tg = env.AddComponent<TerrainGenerator>();

        tg.rockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Gen_Rock.png");
        tg.grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Gen_Grass.png");
        
        // Find Trees
        tg.treePrefabs.Clear();
        string[] treeGuids = AssetDatabase.FindAssets("t:Prefab Tree_0");
        foreach(var guid in treeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("GHIBLI")) // Filter for Ghibli pack if possible
            {
                GameObject tree = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(tree) tg.treePrefabs.Add(tree);
            }
        }
        // Fallback or additional trees
        if (tg.treePrefabs.Count < 5)
        {
             // Add individually if search failed or incomplete
             AddTreeIfFound(tg.treePrefabs, "Tree_01");
             AddTreeIfFound(tg.treePrefabs, "Tree_02");
             AddTreeIfFound(tg.treePrefabs, "Tree_03");
             AddTreeIfFound(tg.treePrefabs, "Tree_04");
             AddTreeIfFound(tg.treePrefabs, "Tree_05");
        }
        
        // Find Rocks/Grass
        tg.rockPrefab = FindPrefab("PP_Rock_Pile_Forest_Moss_05");
        tg.grassPrefab = FindPrefab("PP_Grass_11");

        Debug.Log($"TerrainGenerator Setup: {tg.treePrefabs.Count} Trees assigned.");

        // 3. Setup VegetationSpawner
        VegetationSpawner vs = env.GetComponent<VegetationSpawner>();
        if (!vs) vs = env.AddComponent<VegetationSpawner>();

        vs.treePrefabs = new List<GameObject>(tg.treePrefabs);
        vs.rockPrefab = tg.rockPrefab;
        
        // Assign Player
        GameObject player = GameObject.Find("Player_New");
        if (player) vs.player = player.transform;
        else Debug.LogWarning("Player_New not found in scene!");

        EditorUtility.SetDirty(tg);
        EditorUtility.SetDirty(vs);

        // 4. Force Regenerate
        tg.GenerateTerrain();
        
        // 5. AssetDatabase Refresh
        AssetDatabase.SaveAssets();
        Debug.Log("Environment Fix & Tree Variety Setup Complete!");
    }

    private static void AddTreeIfFound(List<GameObject> list, string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Quick filter to avoid random matches if duplicate names exist
            if (path.Contains("GHIBLI")) 
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go && !list.Contains(go))
                {
                    list.Add(go);
                    return; // Added one, stop
                }
            }
        }
    }

    private static GameObject FindPrefab(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }
}
