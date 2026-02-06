using UnityEngine;
using System.Collections.Generic;

public class VegetationSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject treePrefab;
    public Transform player;
    
    [Header("Generation Params")]
    public float spawnRadiusMin = 20f;
    public float spawnRadiusMax = 50f;
    public int maxTrees = 50;
    public float checkIntervalDistance = 10f; // Check every 10m moved
    public LayerMask groundLayer;
    
    [Header("Scale Variation")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [Header("Legacy / Compatibility")]
    public int treeCount = 80;
    public int grassCount = 400;
    public int rockCount = 50;
    public Vector3 areaSize = new Vector3(100f, 0, 100f);
    public float safeRadius = 15f;
    public GameObject rockPrefab;
    public List<GameObject> treePrefabs = new List<GameObject>();
    
    // Legacy method for Editor tools
    public void SpawnGrass() 
    { 
        SpawnInitialBatch(); 
        Debug.Log("[VegetationSpawner] SpawnGrass (Legacy) triggered SpawnInitialBatch"); 
    }

    private Vector3 m_lastSpawnPos;
    private List<GameObject> m_spawnedTrees = new List<GameObject>();

    private void Start()
    {
        if (player == null)
        {
            // Auto-find player if not assigned
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (player != null)
        {
            m_lastSpawnPos = player.position;
            SpawnInitialBatch();
        }
        else
        {
            Debug.LogError("[VegetationSpawner] Player not found! Ensure Player tag is set.");
        }
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, m_lastSpawnPos);
        if (dist > checkIntervalDistance)
        {
            SpawnChunk();
            m_lastSpawnPos = player.position;
            CullOldTrees();
        }
    }

    private void SpawnInitialBatch()
    {
        // Legacy Support: Ensure treePrefab is set if using the list
        if (treePrefab == null && treePrefabs.Count > 0)
        {
            treePrefab = treePrefabs[0];
        }

        // Initial populate around the player
        for (int i = 0; i < 20; i++) 
        {
            TrySpawnTree();
        }
    }

    private void SpawnChunk()
    {
        // Spawn 5-10 trees per chunk check
        int count = Random.Range(3, 8);
        for (int i = 0; i < count; i++)
        {
            TrySpawnTree();
        }
    }

    private void TrySpawnTree()
    {
        if (treePrefab == null) return;

        // Random Point in Annulus
        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 biomesPos = player.position + new Vector3(circle.x, 0, circle.y); // XZ plane

        // Raycast to find ground height
        Ray ray = new Ray(biomesPos + Vector3.up * 200f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 300f, groundLayer))
        {
            // Check Hit Layer's normal for slope - don't spawn on cliffs
            if (hit.normal.y < 0.5f) return; 

            // Spawn
            Vector3 spawnPos = hit.point;
            Quaternion spawnRot = Quaternion.Euler(0, Random.Range(0, 360), 0);

            GameObject newTree = Instantiate(treePrefab, spawnPos, spawnRot);
            newTree.transform.SetParent(this.transform); // Organization

            // Random Scale
            float scale = Random.Range(minScale, maxScale);
            newTree.transform.localScale = Vector3.one * scale;
            
            m_spawnedTrees.Add(newTree);
        }
    }

    private void CullOldTrees()
    {
        // First cleanup nulls (chopped trees)
        m_spawnedTrees.RemoveAll(t => t == null);

        if (m_spawnedTrees.Count > maxTrees)
        {
            // Remove FIFO (Legacy trees)
            while (m_spawnedTrees.Count > maxTrees)
            {
                GameObject tree = m_spawnedTrees[0];
                if (tree != null)
                {
                    Destroy(tree);
                }
                m_spawnedTrees.RemoveAt(0);
            }
        }
    }
}
