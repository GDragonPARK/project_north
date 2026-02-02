using UnityEngine;
using UnityEditor;

public class TerrainTrigger
{
    [MenuItem("Tools/Regenerate Terrain")]
    public static void Regen()
    {
        TerrainGenerator gen = Object.FindFirstObjectByType<TerrainGenerator>();
        if (gen)
        {
            // Ensure resolution matches user request
            gen.width = 513;
            gen.length = 513;
            gen.GenerateTerrain();
            Debug.Log("Terrain Regenerated Successfully with size 513x513.");
        }
        else
        {
            Debug.LogError("TerrainGenerator component not found in scene!");
        }
    }
}