using UnityEngine;
using UnityEditor;

public class TerrainVerifyAlone
{
    [MenuItem("Tools/Verify Terrain Only")]
    public static void Verify()
    {
        Terrain t = Object.FindFirstObjectByType<Terrain>();
        if (t)
        {
            Debug.Log($"TERRAIN_CHECK: Resolution is {t.terrainData.heightmapResolution}");
            if (t.terrainData.heightmapResolution == 513) Debug.Log("TERRAIN_CHECK: SUCCESS (513)");
            else Debug.LogError($"TERRAIN_CHECK: FAIL ({t.terrainData.heightmapResolution})");
        }
        else
        {
            Debug.LogError("TERRAIN_CHECK: No Terrain Found");
        }
    }
}