using UnityEngine;
using UnityEditor;

public class SpawnPointBuilder : EditorWindow
{
    [MenuItem("Tools/Build Spawn Point")]
    public static void Build()
    {
        // 1. Create Root
        GameObject spawnRoot = new GameObject("Spawn_Point");
        
        // 2. Find Assets (Ghibli)
        GameObject altarPrefab = FindAssetByName("Altar t:Prefab");
        GameObject archPrefab = FindAssetByName("Arch t:Prefab");
        GameObject rockPrefab1 = FindAssetByName("Rock_01 t:Prefab");
        GameObject rockPrefab2 = FindAssetByName("Rock_02 t:Prefab");

        if (altarPrefab)
        {
            GameObject altar = (GameObject)PrefabUtility.InstantiatePrefab(altarPrefab, spawnRoot.transform);
            altar.transform.localPosition = Vector3.zero;
        }
        else Debug.LogError("Altar prefab not found!");

        if (archPrefab)
        {
            GameObject arch = (GameObject)PrefabUtility.InstantiatePrefab(archPrefab, spawnRoot.transform);
            arch.transform.localPosition = new Vector3(0, 0, 4f); // 4m forward
            arch.transform.localRotation = Quaternion.identity;
        }

        if (rockPrefab1)
        {
            GameObject r1 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab1, spawnRoot.transform);
            r1.transform.localPosition = new Vector3(-3, 0, 2);
            r1.transform.localRotation = Quaternion.Euler(0, 45, 0);
        }
        
        if (rockPrefab2)
        {
            GameObject r2 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab2, spawnRoot.transform);
            r2.transform.localPosition = new Vector3(3, 0, 2);
            r2.transform.localRotation = Quaternion.Euler(0, -45, 0);
        }

        // 3. Place on Terrain
        PlaceOnTerrain(spawnRoot);

        Selection.activeGameObject = spawnRoot;
        Debug.Log("Spawn Point Built!");
    }

    static GameObject FindAssetByName(string filter)
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        // Prefer "GHIBLI" path if multiple
        foreach(var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains("GHIBLI") && p.Contains("Prefabs")) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        // Fallback to first
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    static void PlaceOnTerrain(GameObject obj)
    {
        Terrain t = Object.FindFirstObjectByType<Terrain>();
        if (t)
        {
            float x = 0; // Center world
            float z = 0;
            float y = t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y;
            obj.transform.position = new Vector3(x, y, z);
            
            // Align rotation to flat? Or keep identity?
            // Valheim altars are usually flat even on hills, or terrain is flattened.
            // For now, simple position placement.
        }
    }
}