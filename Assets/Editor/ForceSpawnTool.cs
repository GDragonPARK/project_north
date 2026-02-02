using UnityEngine;
using UnityEditor;

public class ForceSpawnTool : EditorWindow
{
    [MenuItem("Tools/Force Spawn Player")]
    public static void ForceSpawn()
    {
        // 1. Find or Create Player_New
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            // Find Knight Prefab - Try "Knight" model first
            string[] guids = AssetDatabase.FindAssets("Knight t:Model");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("Knight"); // Fallback

            if (guids.Length > 0)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                player.name = "Player_New";
                
                // Add essential components if fresh
                if (!player.GetComponent<CharacterController>()) 
                {
                    var cc = player.AddComponent<CharacterController>();
                    cc.center = new Vector3(0, 0.9f, 0);
                    cc.height = 1.8f;
                    cc.radius = 0.28f;
                }
                Debug.Log("Created new Player_New from Knight prefab.");
            }
            else
            {
                Debug.LogError("Knight prefab not found!");
                return;
            }
        }

        // 2. Find and Place Spawn Point
        GameObject spawn = GameObject.Find("Spawn_Point");
        Vector3 spawnPos = Vector3.zero;
        
        // Terrain Height lookup
        Terrain t = Object.FindFirstObjectByType<Terrain>();
        if (t)
        {
            float y = t.SampleHeight(new Vector3(0, 0, 0)) + t.transform.position.y;
            spawnPos = new Vector3(0, y, 0);
        }

        if (spawn)
        {
            spawn.transform.position = spawnPos;
            Debug.Log($"Moved Spawn_Point to {spawnPos}");
        }
        else
        {
            Debug.LogWarning("Spawn_Point object not found. Placing Player at terrain zero height directly.");
        }

        // 3. Move Player
        player.transform.position = spawnPos;
        player.transform.rotation = Quaternion.identity;
        Debug.Log($"Moved Player_New to {spawnPos}");

        // 4. Focus
        Selection.activeGameObject = player;
        if (SceneView.lastActiveSceneView)
        {
            SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log("Focused Scene Camera on Player.");
        }
    }
}