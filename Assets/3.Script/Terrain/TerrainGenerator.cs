using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(TerrainCollider))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("1. 지형 조절 버튼 (Terrain Settings)")]
    [Tooltip("산의 높이! 10~20 사이가 적당해요.")]
    public float amplitude = 8f; // Ghibli style: Flatter

    [Tooltip("산맥의 촘촘함! 0.01~0.05 사이가 부드러운 언덕이 돼요.")]
    public float frequency = 0.012f; // Smoother

    [Header("2. 심을 물건들 (Prefabs)")]
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject grassPrefab;



    [Header("4. 생성 설정 (Fixed Resolution)")]
    public int width = 513; // Fixed for Out of Bounds Error
    public int length = 513;
    public int objectDensity = 2000;

    private Terrain m_terrain;
    private GameObject natureRoot;

    // Ghibli Textures
    public Texture2D rockTexture;
    public Texture2D grassTexture;

    [ContextMenu("Generate Terrain")] 
    public void GenerateTerrain()
    {
        // 0. Cleanup
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (m_terrain == null) m_terrain = GetComponent<Terrain>();
        if (m_terrain.terrainData == null) m_terrain.terrainData = new TerrainData();

        // 1. TerrainData Init (Power of 2 + 1)
        m_terrain.terrainData.heightmapResolution = width; // 513
        m_terrain.terrainData.size = new Vector3(width, amplitude, length);
        m_terrain.terrainData.alphamapResolution = width;

        // Force Collider Update
        TerrainCollider tc = GetComponent<TerrainCollider>();
        if (tc) tc.terrainData = m_terrain.terrainData;

        // 2. HeightMap
        float[,] heights = CalculateHeights();
        m_terrain.terrainData.SetHeights(0, 0, heights);

        // 3. Texture Splatmap (Rock vs Grass)
        ApplyTextures(heights);

        // 4. Object Spawning is now handled by VegetationSpawner mostly, 
        // but if we keep it here as a backup/companion:
        // SpawnObjects(heights); 
        // User asked to clean legacy, but VegetationSpawner is separate. 
        // Let's rely on VegetationSpawner for vegetation.
        Debug.Log("Ghibli Terrain Generated!");
    }

    private float[,] CalculateHeights()
    {
        int res = m_terrain.terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        Vector2 offset = new Vector2(Random.Range(0f, 9999f), Random.Range(0f, 9999f));

        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                float xCoord = (float)x / res * width * frequency + offset.x;
                float zCoord = (float)z / res * length * frequency + offset.y;
                float noise = Mathf.PerlinNoise(xCoord, zCoord);
                
                // Flatten near (0,0) - Assuming Terrain starts at (0,0) or we map it
                // Logic: Distance from 0,0 (Corner) or Center? 
                // Let's assume user wants World (0,0) to be flat. If Terrain is at (0,0,0), that's index 0,0.
                // We dampen height near 0,0
                float dist = Mathf.Sqrt(x*x + z*z);
                float flattenRadius = 50f; // 50 units flat
                float flattenFactor = Mathf.Clamp01((dist - 20) / flattenRadius); // 0 near origin, 1 away
                
                heights[x, z] = noise * flattenFactor; // Flatten center
            }
        }
        return heights;
    }

    private void ApplyTextures(float[,] heights)
    {
        if (rockTexture == null || grassTexture == null) 
        {
            Debug.LogWarning("Assign Rock and Grass textures in Inspector!");
            return;
        }

        TerrainLayer rockLayer = new TerrainLayer { 
            diffuseTexture = rockTexture, 
            tileSize = new Vector2(3, 3) 
        };
        TerrainLayer grassLayer = new TerrainLayer { 
            diffuseTexture = grassTexture, 
            tileSize = new Vector2(3, 3) 
        };

        m_terrain.terrainData.terrainLayers = new TerrainLayer[] { rockLayer, grassLayer };

        int alphamapWidth = m_terrain.terrainData.alphamapResolution;
        int alphamapHeight = m_terrain.terrainData.alphamapHeight;

        float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, 2];

        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                // Normalize coordinates
                float normX = x * 1.0f / (alphamapWidth - 1);
                float normY = y * 1.0f / (alphamapHeight - 1);

                // Sample height (approximate from heights array or re-sample)
                // Since resolutions match, we can direct map or simple lerp
                // Let's use the heights array passed in (checking bounds)
                float height = heights[Mathf.Clamp(x, 0, heights.GetLength(0)-1), Mathf.Clamp(y, 0, heights.GetLength(1)-1)];
                
                // Logic: y < 2 (normalized depends on amplitude). 
                // User said: y < 2. Amplitude is usually 15. So 2/15 approx 0.13
                float threshold = 2.0f / amplitude;

                if (height < threshold)
                {
                    splatmapData[x, y, 0] = 1; // Rock
                    splatmapData[x, y, 1] = 0;
                }
                else
                {
                    splatmapData[x, y, 0] = 0;
                    splatmapData[x, y, 1] = 1; // Grass
                }
            }
        }
        m_terrain.terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}

