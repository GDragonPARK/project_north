using UnityEngine;
using System.Collections.Generic;

public class VegetationSpawner : MonoBehaviour
{
    [Header("Ghibli Assets")]
    public List<GameObject> treePrefabs;
    public List<GameObject> flowerPrefabs;
    public List<GameObject> grassPrefabs;
    public GameObject rockPrefab;
    
    [Header("Settings")]
    public int treeCount = 80;
    public int grassCount = 400;
    public int rockCount = 50;

    public Vector3 areaSize = new Vector3(500, 0, 500);
    public float maxSlope = 30f;
    public float safeRadius = 15.0f; // No spawn near player
    
    // Noise settings for clustering
    public float noiseScale = 0.05f;

    [Header("Optimization")]
    public float cullDistance = 150f;
    public bool enableRuntimeCulling = true;
    public Transform playerTransform;

    private float _timer;

    private void Start()
    {
        if (playerTransform == null)
        {
            var controller = Object.FindFirstObjectByType<MyPlayerController>();
            if (controller != null) playerTransform = controller.transform;
        }
    }

    private void Update()
    {
        if (!enableRuntimeCulling || playerTransform == null) return;

        _timer += Time.deltaTime;
        if (_timer > 0.5f) // Check every 0.5s
        {
            _timer = 0;
            OptimizeVisibleVegetation();
        }
    }

    private void OptimizeVisibleVegetation()
    {
        Vector3 playerPos = playerTransform.position;
        // Optimization: Use sqrMagnitude to avoid Sqrt
        float distSq = cullDistance * cullDistance;

        foreach(Transform child in transform)
        {
            float d2 = (child.position - playerPos).sqrMagnitude;
            bool shouldBeActive = d2 < distSq;
            
            if (child.gameObject.activeSelf != shouldBeActive)
            {
                child.gameObject.SetActive(shouldBeActive);
            }
        }
    }

    [ContextMenu("Spawn Vegetation")]
    public void SpawnGrass()
    {
        // Cleanup existing
        var children = new List<GameObject>();
        foreach(Transform child in transform) children.Add(child.gameObject);
        foreach(var c in children) DestroyImmediate(c);

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        // 1. Spawn Trees
        SpawnObjects(treePrefabs, treeCount, 0.7f, 2.0f); // Trees need space

        // 2. Spawn Rocks
        SpawnObjects(new List<GameObject>{ rockPrefab }, rockCount, 0.5f, 1.5f); // Rocks, assume rockPrefab is in list or use single

        // 3. Spawn Grass
        SpawnObjects(grassPrefabs, grassCount, 0.1f, 1.0f); // Grass is dense

        Debug.Log($"Spawned Ghibli World: {treeCount} Trees, {rockCount} Rocks, {grassCount} Grass.");
    }

    private void SpawnObjects(List<GameObject> prefabs, int count, float noiseThreshold, float scaleMod)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 20;

        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;
            float x = Random.Range(-areaSize.x/2, areaSize.x/2);
            float z = Random.Range(-areaSize.z/2, areaSize.z/2);
            Vector3 pos = transform.position + new Vector3(x, 200, z);

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 500f))
            {
                // Checks
                if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;
                if (playerTransform != null && Vector3.Distance(hit.point, playerPos) < safeRadius) continue; // Safety Zone
                
                // Optional: Noise check for clustering (if desired, or random)
                // User asked for "Randomly", but clustering looks better. Let's keep it random for now to ensure counts.
                
                spawned++;
                GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                if (prefab == null) continue;

                GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                if (obj == null) obj = Instantiate(prefab);

                obj.transform.position = hit.point;
                obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                obj.transform.SetParent(transform);
                
                float s = Random.Range(0.8f, 1.2f) * scaleMod;
                obj.transform.localScale = Vector3.one * s;

                // Shader/LOD logic setup...
                SetupVisuals(obj);
            }
        }
    }

    private void SetupVisuals(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        if (r != null)
        {
             MaterialPropertyBlock block = new MaterialPropertyBlock();
             r.GetPropertyBlock(block);
             
             block.SetColor("_BaseColor", Color.white * Random.Range(0.85f, 1.15f)); 
             block.SetFloat("_WindStrength", 2.0f);
             block.SetFloat("_WindSpeed", 1.5f);
             
             r.SetPropertyBlock(block);

             if (obj.GetComponent<LODGroup>() == null)
             {
                 LODGroup lodGroup = obj.AddComponent<LODGroup>();
                 LOD[] lods = new LOD[1];
                 Renderer[] renderers = new Renderer[] { r };
                 lods[0] = new LOD(0.05f, renderers); 
                 lodGroup.SetLODs(lods);
                 lodGroup.RecalculateBounds();
             }
        }
    }
            

}
