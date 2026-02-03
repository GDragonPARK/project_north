using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ForceTerrainParams : EditorWindow
{
    [MenuItem("Tools/Force Terrain Params & Spawn")]
    public static void ForceApply()
    {
        // 1. Modify TerrainGenerator
        TerrainGenerator gen = Object.FindFirstObjectByType<TerrainGenerator>();
        if (gen)
        {
            SerializedObject so = new SerializedObject(gen);
            so.Update();
            so.FindProperty("amplitude").floatValue = 15f;
            so.FindProperty("frequency").floatValue = 0.005f;
            so.ApplyModifiedProperties();
            
            gen.GenerateTerrain();
            Debug.Log("Terrain Regenerated with Freq=0.005, Amp=15 in Scene");
        }
        else Debug.LogError("TerrainGenerator not found");

        // 2. Flatten Center Manually
        Terrain t = Terrain.activeTerrain;
        if (t)
        {
            int res = t.terrainData.heightmapResolution;
            float[,] heights = t.terrainData.GetHeights(0, 0, res, res);
            Vector3 size = t.terrainData.size;
            Vector3 pos = t.transform.position;
            
            // World 0,0 to Local normalized
            float nX = (0 - pos.x) / size.x;
            float nZ = (0 - pos.z) / size.z;
            
            int cX = Mathf.RoundToInt(nX * (res - 1));
            int cY = Mathf.RoundToInt(nZ * (res - 1));
            int r = 10; // 10 units/pixels
            
            for (int x = cX - r; x <= cX + r; x++)
            {
                for (int y = cY - r; y <= cY + r; y++)
                {
                    if (x >= 0 && x < res && y >= 0 && y < res)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(cX, cY)) <= r)
                        {
                            heights[y, x] = 0f;
                        }
                    }
                }
            }
            t.terrainData.SetHeights(0, 0, heights);
            Debug.Log("Flattened Center (0,0)");
        }

        // 3. Upgrade Altar
        GameObject altar = GameObject.Find("Spawn_Point");
        if (altar == null) altar = GameObject.FindGameObjectWithTag("Respawn");
        if (altar)
        {
            altar.transform.localScale = new Vector3(3, 3, 3);
            altar.transform.position = Vector3.zero;
            altar.transform.rotation = Quaternion.identity;
            EditorUtility.SetDirty(altar);
        }

        // 4. Force Player
        GameObject player = GameObject.Find("Player_New");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            
            player.transform.position = new Vector3(0, 0.1f, 0); // On ground 0
            player.transform.rotation = Quaternion.identity;
            
            if (cc) cc.enabled = true;
            EditorUtility.SetDirty(player);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Force Fix Complete. Scene Saved.");
    }
}
