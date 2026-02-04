using UnityEngine;
using StarterAssets;
// using Cinemachine; 

public class PlayerSpawner : MonoBehaviour
{
    [Header("Settings")]
    public string spawnPointName = "Spawn_Point";
    public Vector3 spawnOffset = new Vector3(0, 5.0f, 5.0f); // 제단 정중앙에서 5.0m 위로 설정 (완전한 낙하 보장)
    public bool forceCameraReset = true;

    void Start()
    {
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        GameObject spawn = GameObject.Find(spawnPointName);
        if (spawn != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player_New");

            if (player)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;

                // 수정된 부분: 제단의 위치와 회전을 고려하여 로컬 좌표를 월드 좌표로 변환
                // Y값을 5.0f 정도로 높여서 지붕 위 공중에서 떨어지게 만듭니다.
                Vector3 safeSpawnPos = spawn.transform.TransformPoint(new Vector3(0, 5.0f, 0));

                player.transform.position = safeSpawnPos;
                player.transform.rotation = spawn.transform.rotation;

                if (cc) cc.enabled = true;
                Debug.Log("Player Spawned safely ABOVE Altar.");
            }
        }
    }

    [ContextMenu("Auto Spawn Setup")]
    public void AutoSpawnSetup()
    {
        #if UNITY_EDITOR
        // 1. Find or Create Spawn Point (Altar)
        GameObject altar = GameObject.Find(spawnPointName);
        if (altar == null)
        {
            // Try to load Ghibli Altar
            string[] guids = UnityEditor.AssetDatabase.FindAssets("Altar t:Prefab");
            foreach (var g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains("GHIBLI"))
                {
                    GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab)
                    {
                         altar = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                         altar.name = spawnPointName;
                         break;
                    }
                }
            }
        }
        
        if (altar == null)
        {
            altar = new GameObject(spawnPointName);
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(altar.transform);
            cylinder.transform.localScale = new Vector3(2, 0.2f, 2);
            Debug.LogWarning("Created placeholder Altar (Ghibli prefab not found).");
        }
        
        // Scale Up Altar
        altar.transform.localScale = new Vector3(2, 2, 2);

        // 2. Find Flat Spot (Narrow Search near 0,0)
        Vector3 bestPos = Vector3.zero;
        float minSlope = float.MaxValue;
        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null)
        {
            for (int x = -5; x <= 5; x++) // Very narrow search
            {
                for (int z = -5; z <= 5; z++)
                {
                    float worldX = x;
                    float worldZ = z;
                    float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrain.transform.position.y;
                    
                    // Simple Slope Check
                    float h1 = terrain.SampleHeight(new Vector3(worldX + 1, 0, worldZ));
                    float h2 = terrain.SampleHeight(new Vector3(worldX - 1, 0, worldZ));
                    
                    float slope = Mathf.Abs(y - h1) + Mathf.Abs(y - h2);
                    
                    if (slope < minSlope)
                    {
                        minSlope = slope;
                        bestPos = new Vector3(worldX, y, worldZ);
                    }
                }
            }
        }
        else
        {
            bestPos = new Vector3(0, 0, 0); 
        }
        
        // 3. Place Altar
        altar.transform.position = bestPos;
        altar.transform.rotation = Quaternion.identity; 

        // 4. Place Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player_New");
        
        if (player)
        {
             CharacterController cc = player.GetComponent<CharacterController>();
             if (cc) cc.enabled = false;
             
             player.transform.position = bestPos + new Vector3(0, 1.0f * altar.transform.localScale.y, 0); // Adjust for scale
             player.transform.rotation = altar.transform.rotation;
             
             if (cc) cc.enabled = true;
        }

        // 5. Align Camera
        Camera mainCam = Camera.main;
        if (mainCam)
        {
            // Position camera further back and higher
            mainCam.transform.position = bestPos + new Vector3(0, 8, -12);
            mainCam.transform.LookAt(bestPos + Vector3.up * 2);
        }
            
            // If Cinemachine used, finding the FreeLook and resetting it might be needed, 
            // but simply placing the camera often helps the initial brain blend.

        // 6. Save
        UnityEditor.EditorUtility.SetDirty(altar);
        if (player) UnityEditor.EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log($"Auto Spawn Setup Complete. Altar at {bestPos}. Scene Saved.");
        #endif
    }

    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Auto Spawn Setup")]
    public static void RunAutoSpawnSetup()
    {
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner) 
        {
            spawner.AutoSpawnSetup();
        }
        else
        {
            Debug.LogWarning("No PlayerSpawner found in scene!");
        }
    }
    #endif
}