using UnityEngine;
using System.Collections.Generic;

public class VegetationSpawner : MonoBehaviour
{
    [Header("Settings")]
    public List<GameObject> treePrefabs = new List<GameObject>();
    public List<GameObject> flowerPrefabs = new List<GameObject>(); // Added
    public List<GameObject> shrubPrefabs = new List<GameObject>(); // Added
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
    
    // Legacy method for Editor tools
    public void SpawnGrass() 
    { 
        SpawnAllVegetationEditor();
        Debug.Log("[VegetationSpawner] SpawnGrass triggered SpawnAllVegetationEditor"); 
    }

    // Called by TerrainGenerator in Editor
    public void SpawnAllVegetationEditor()
    {
        // Legacy bridge, fallback to small area if width/length unknown
        SpawnFullWorld(2049, 2049, 8000); 
    }

    public void SpawnFullWorld(float width, float length, int density)
    {
        Debug.Log($"[VegetationSpawner] Starting Full World CLUSTER Gen: {width}x{length}, Density={density}");

        // DEBUG: Check prefab list counts
        Debug.Log($"[VegetationSpawner] DEBUG: treePrefabs.Count = {treePrefabs?.Count ?? -1}");
        Debug.Log($"[VegetationSpawner] DEBUG: flowerPrefabs.Count = {flowerPrefabs?.Count ?? -1}");
        Debug.Log($"[VegetationSpawner] DEBUG: shrubPrefabs.Count = {shrubPrefabs?.Count ?? -1}");

        // Clear previous children
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            if (Application.isEditor) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
        }

        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 1. Ratio Setup (40% Trees, 40% Shrubs, 20% Flowers)
        int treeCount = (int)(density * 0.4f);
        int shrubCount = (int)(density * 0.4f);
        int flowerCount = (int)(density * 0.2f);
        
        // 2. Spawn Clusters
        // Trees (Forests) - Often have Shrubs mixed in
        SpawnClusterGroup(treePrefabs, width, length, treeCount, isTree: true, companionPrefabs: shrubPrefabs);
        
        // Shrubs (Thickets) - Independent clusters to fill gaps
        SpawnClusterGroup(shrubPrefabs, width, length, shrubCount, isTree: false, companionPrefabs: null);

        // Flowers (Meadows) - Small patches
        SpawnClusterGroup(flowerPrefabs, width, length, flowerCount, isTree: false, companionPrefabs: null);

        Debug.Log($"[VegetationSpawner] Cluster Gen Complete. Approx {transform.childCount} objects.");
    }
    
    private void SpawnClusterGroup(List<GameObject> prefabs, float width, float length, int totalCount, bool isTree, List<GameObject> companionPrefabs)
    {
         if (prefabs == null || prefabs.Count == 0) return;

         int currentCount = 0;
         int consecutiveFails = 0;

         // Fix: Loop limit changed to check CONSECUTIVE failures, not total. 
         // This ensures it keeps trying until it really can't find a spot.
         while (currentCount < totalCount && consecutiveFails < 1000)
         {
             // 1. Pick a Cluster Center
             float cx = Random.Range(0f, width);
             float cz = Random.Range(0f, length);
             Vector3 center = new Vector3(cx, 0, cz);

             // Verify center valid (Height/Slope)
             if (!IsValidPosition(center, isTree, out Vector3 groundCenter)) 
             {
                 consecutiveFails++;
                 continue;
             }
             
             consecutiveFails = 0; // Reset on valid center

             // 2. Spawn Cluster Loop
             int clusterSize = Random.Range(3, 8); // 3~8 objects per cluster
             float clusterRadius = Random.Range(5f, 12f); // 5~12m radius

             for (int i = 0; i < clusterSize; i++)
             {
                 if (currentCount >= totalCount) break;

                 Vector2 offset = Random.insideUnitCircle * clusterRadius;
                 Vector3 pos = groundCenter + new Vector3(offset.x, 0, offset.y);
                 
                 // Raycast for individual object
                 if (IsValidPosition(pos, isTree, out Vector3 hitPoint))
                 {
                     GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Count)];
                     SpawnSingle(prefabToSpawn, hitPoint);
                     currentCount++;

                     // Companion Logic (Tree -> Shrub)
                     if (isTree && companionPrefabs != null && companionPrefabs.Count > 0 && Random.value < 0.4f)
                     {
                         // Spawn a shrub nearby
                         Vector2 subOffset = Random.insideUnitCircle * 2f;
                         Vector3 subPos = hitPoint + new Vector3(subOffset.x, 0, subOffset.y);
                         if (IsValidPosition(subPos, false, out Vector3 subHit))
                         {
                             GameObject companion = companionPrefabs[Random.Range(0, companionPrefabs.Count)];
                             SpawnSingle(companion, subHit);
                             // Does not count towards main limit
                         }
                     }
                 }
                 else
                 {
                     consecutiveFails++; 
                     if (consecutiveFails > 1000) break; // Break inner loop too
                 }
             }
         }
         
         if (consecutiveFails >= 1000)
         {
             Debug.LogWarning($"[VegetationSpawner] Gave up spawning {isTree} (Consecutive Fails > 1000). Spawned/Total: {currentCount}/{totalCount}");
         }
    }

    private bool IsValidPosition(Vector3 pos, bool isTree, out Vector3 groundPos)
    {
        groundPos = Vector3.zero;
        // Fix: Raise raycast origin to 2000f to catch high terrain
        Ray ray = new Ray(new Vector3(pos.x, 2000f, pos.z), Vector3.down);
        
        // Fix: Force Layer Mask 9 (Terrain) & 0 (Default) regardless of Inspector
        int mask = groundLayer.value | (1 << 9) | (1 << 0); 
        if (mask == 0) mask = LayerMask.GetMask("Default", "Terrain", "Ground");

        // Trace 3000f down to cover full range
        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, mask))
        {
            groundPos = hit.point;
            
            // Safe Zone Check
            if (Vector3.Distance(groundPos, Vector3.zero) < 50f) return false;

            // Fix: Lower height limit to -100f
            if (groundPos.y < -100f) return false; 

            // Slope Check
            if (isTree && hit.normal.y < 0.7f) return false;
            if (!isTree && hit.normal.y < 0.5f) return false;

            return true;
        }
        return false;
    }

    private void SpawnSingle(GameObject prefab, Vector3 pos)
    {
        GameObject instance = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0));
        instance.transform.SetParent(this.transform);
        float scale = Random.Range(minScale, maxScale);
        instance.transform.localScale = Vector3.one * scale;
    }

    // Unified Spawn Helper (Radius based - runtime)
    private void TrySpawnObject(List<GameObject> prefabs, Vector3 center, float radius)
    {
        // Runtime localized spawning logic (Radius based)
        // Kept for Update() loop runtime usage
        if (prefabs == null || prefabs.Count == 0) return;

        Vector2 circle = Random.insideUnitCircle * radius;
        Vector3 pos = center + new Vector3(circle.x, 0, circle.y);
        
        // Fix: Raise raycast origin to 2000f to catch high terrain
        Ray ray = new Ray(pos + Vector3.up * 2000f, Vector3.down);
        
        // Fix: Force Layer Mask 9 (Terrain) & 0 (Default)
        int mask = groundLayer.value == 0 ? LayerMask.GetMask("Default", "Terrain", "Ground") : groundLayer.value | (1 << 9) | (1 << 0);

        // Trace 3000f down
        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, mask))
        {
             if (hit.point.y < -100f) return; // Fix: Height check relaxed to -100f
             if (hit.normal.y < 0.5f) return; // Slope check

             GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
             GameObject instance = Instantiate(prefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
             instance.transform.SetParent(this.transform);
             
             float scale = Random.Range(minScale, maxScale);
             instance.transform.localScale = Vector3.one * scale;
             
             // Add to list for checking limits
             m_spawnedTrees.Add(instance);
        }
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

        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain");

        if (player != null)
        {
            m_lastSpawnPos = player.position;
            // Runtime spawning only if not already populated by Editor?
            // User requested "Editor Spawning". Usually we don't want double spawn.
            if (transform.childCount == 0) SpawnInitialBatch();
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
        TrySpawnObject(treePrefabs, player.position, Random.Range(spawnRadiusMin, spawnRadiusMax));
    }

    private void CullOldTrees()
    {
        // Simplistic culling based on distance or count
        // For now, keep it simple as user requested Editor Spawning focus
        if (transform.childCount > maxTrees * 2)
        {
             // Remove oldest
             if (transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);
        }
    }
}
