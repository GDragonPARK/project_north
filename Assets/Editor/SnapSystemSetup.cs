using UnityEngine;
using UnityEditor;

public class SnapSystemSetup
{
    [MenuItem("Tools/Building System/Setup Snap Prefabs")]
    public static void SetupSnapPrefabs()
    {
        SetupPrefab("Assets/Resources/Building/Recipes/Wood_Floor.asset", "Wood_Floor", SnapType.Floor);
        SetupPrefab("Assets/Resources/Building/Recipes/Wood_Wall.asset", "Wood_Wall", SnapType.Wall);
    }

    private static void SetupPrefab(string recipePath, string prefabName, SnapType type)
    {
        BuildRecipeSO recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeSO>(recipePath);
        if (recipe == null || recipe.prefab == null) 
        {
            Debug.LogError($"Recipe or Prefab not found for {prefabName}");
            return;
        }

        GameObject prefab = recipe.prefab;
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (!prefabPath.EndsWith(".prefab"))
        {
            Debug.LogWarning($"[SnapSystemSetup] Skipping {prefabName} - it is not a .prefab file (Path: {prefabPath})");
            return;
        }
        
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editScope.prefabContentsRoot;

            // 1. Cleanup Missing Scripts (Fixes "Missing Script" save errors)
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removed > 0) Debug.LogWarning($"Removed {removed} missing scripts from {prefabName}");

            // 2. Cleanup Old SnapPoints Container
            Transform oldContainer = root.transform.Find("SnapPoints");
            if (oldContainer != null)
            {
                Object.DestroyImmediate(oldContainer.gameObject);
            }

            // 3. Cleanup loose SnapPoints (if any remain)
            var existingSnaps = root.GetComponentsInChildren<SnapPoint>();
            foreach (var sp in existingSnaps) Object.DestroyImmediate(sp.gameObject);

            // 4. Add BuildingPiece
            if (!root.GetComponent<BuildingPiece>()) root.AddComponent<BuildingPiece>();

            // 5. Create SnapPoints Container
            GameObject snapContainer = new GameObject("SnapPoints");
            snapContainer.transform.SetParent(root.transform, false);

            if (type == SnapType.Floor)
            {
                CreateSnapPoint(snapContainer, new Vector3(0, 0, 1), Quaternion.identity, SnapType.Floor, "Floor_Front"); 
                CreateSnapPoint(snapContainer, new Vector3(0, 0, -1), Quaternion.Euler(0, 180, 0), SnapType.Floor, "Floor_Back"); 
                CreateSnapPoint(snapContainer, new Vector3(-1, 0, 0), Quaternion.Euler(0, -90, 0), SnapType.Floor, "Floor_Left"); 
                CreateSnapPoint(snapContainer, new Vector3(1, 0, 0), Quaternion.Euler(0, 90, 0), SnapType.Floor, "Floor_Right"); 
            }
            else if (type == SnapType.Wall)
            {
                CreateSnapPoint(snapContainer, new Vector3(0, 0, 0), Quaternion.identity, SnapType.Floor, "Wall_Bottom"); 
                CreateSnapPoint(snapContainer, new Vector3(0, 2, 0), Quaternion.identity, SnapType.Wall, "Wall_Top"); 
                CreateSnapPoint(snapContainer, new Vector3(-1, 0, 0), Quaternion.Euler(0, -90, 0), SnapType.Wall, "Wall_Left"); 
                CreateSnapPoint(snapContainer, new Vector3(1, 0, 0), Quaternion.Euler(0, 90, 0), SnapType.Wall, "Wall_Right"); 
            }

            Debug.Log($"Setup complete for {prefabName}");
        }
    }

    private static void CreateSnapPoint(GameObject parent, Vector3 pos, Quaternion rot, SnapType type, string id)
    {
        GameObject go = new GameObject($"SnapPoint_{type}_{id}");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.layer = LayerMask.NameToLayer("SnapPoint");

        SnapPoint sp = go.AddComponent<SnapPoint>();
        sp.snapType = type;
        sp.socketId = id;
        sp.snapRadius = 0.25f; 
        
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.1f;
    }
}
