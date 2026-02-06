using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildingSystemSetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Building System Phase 1")]
    public static void Setup()
    {
        // 1. Ensure BuildingManager exists
        BuildingManager bm = Object.FindFirstObjectByType<BuildingManager>();
        if (bm == null)
        {
            GameObject go = new GameObject("BuildingManager");
            bm = go.AddComponent<BuildingManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create BuildingManager");
        }

        // 2. Find Prefabs (Heuristic search)
        if (bm.floorPrefab == null) bm.floorPrefab = FindPrefab("Floor_Wood"); // KayKit?
        if (bm.floorPrefab == null) bm.floorPrefab = FindPrefab("Floor");

        if (bm.wallPrefab == null) bm.wallPrefab = FindPrefab("Wall_Wood");
        if (bm.wallPrefab == null) bm.wallPrefab = FindPrefab("Wall");

        // 3. Add SnapPoints if needed (Editing the Prefab asset directly is risky, instantiate wrapper?)
        // Better: We edit the PREFAB if the user confirms, or we assume these are our custom ones.
        // Let's trying to Find "Building_Floor" or "Building_Wall" specifically for this project.
        
        Debug.Log($"[BuildingSetup] Floor: {bm.floorPrefab}, Wall: {bm.wallPrefab}");
        
        if (bm.floorPrefab) AddSnapPoints(bm.floorPrefab);
        if (bm.wallPrefab) AddSnapPoints(bm.wallPrefab);

        Selection.activeGameObject = bm.gameObject;
    }

    private static GameObject FindPrefab(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }

    private static void AddSnapPoints(GameObject prefab)
    {
        // Load Prefab contents
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        // Check if SnapPoints exist
        if (contents.GetComponentInChildren<SnapPoint>())
        {
            PrefabUtility.UnloadPrefabContents(contents);
            return; // Already setup
        }

        Renderer r = contents.GetComponentInChildren<Renderer>();
        if (r)
        {
            Bounds b = r.bounds; // Local bounds relative to prefab root (if root is 0,0,0)
            
            // Heuristic for Floor vs Wall based on bounds
            bool isFlat = b.size.y < b.size.x && b.size.y < b.size.z;
            
            List<Vector3> points = new List<Vector3>();
            Vector3 c = b.center;
            Vector3 e = b.extents;

            if (isFlat) // Floor
            {
                points.Add(c + new Vector3(e.x, 0, e.z));
                points.Add(c + new Vector3(-e.x, 0, e.z));
                points.Add(c + new Vector3(e.x, 0, -e.z));
                points.Add(c + new Vector3(-e.x, 0, -e.z));
                
                // Add midpoints for chaining
                points.Add(c + new Vector3(e.x, 0, 0));
                points.Add(c + new Vector3(-e.x, 0, 0));
                points.Add(c + new Vector3(0, 0, e.z));
                points.Add(c + new Vector3(0, 0, -e.z));
            }
            else // Wall (Assume Z is thickness, X is width, Y is height)
            {
                // Bottom Corners
                points.Add(c + new Vector3(e.x, -e.y, 0));
                points.Add(c + new Vector3(-e.x, -e.y, 0));
                // Top Corners
                points.Add(c + new Vector3(e.x, e.y, 0));
                points.Add(c + new Vector3(-e.x, e.y, 0));
                // Sides?
                points.Add(c + new Vector3(e.x, 0, 0));
                points.Add(c + new Vector3(-e.x, 0, 0));
            }

            foreach (var p in points)
            {
                GameObject sp = new GameObject("SnapPoint");
                sp.transform.SetParent(contents.transform);
                sp.transform.localPosition = p;
                sp.AddComponent<SnapPoint>();
            }
        }

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        Debug.Log($"[BuildingSetup] Added SnapPoints to {prefab.name}");
    }
}
