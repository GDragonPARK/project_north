using UnityEngine;
using UnityEditor;

public class WoodItemVisualUpdater : EditorWindow
{
    [MenuItem("Antigravity/💎 Upgrade Wood Item Visuals")]
    public static void UpgradeWoodVisuals()
    {
        string woodPrefabPath = "Assets/Prefabs/Wood.prefab";
        GameObject woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(woodPrefabPath);

        if (woodPrefab == null)
        {
            Debug.LogError("Could not find Wood.prefab at " + woodPrefabPath);
            return;
        }

        // 1. High Quality Mesh Replacement (Using Box from Ghibli pack as a 'Wood Box' or finding mesh)
        // Let's use the actual Box model as a placeholder or search for the submesh of Tree_02
        string meshPath = "Assets/99.ThirdParty/3D set of stylized nature - GHIBLI style/Art/Meshes/Box.fbx";
        GameObject boxSource = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
        
        if (boxSource == null)
        {
             Debug.LogError("Could not find Box source mesh at " + meshPath);
             return;
        }

        Mesh sourceMesh = boxSource.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        Material sourceMat = boxSource.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;

        // Open Prefab for editing
        GameObject instance = PrefabUtility.LoadPrefabContents(woodPrefabPath);
        
        try {
            // Update Mesh
            MeshFilter mf = instance.GetComponent<MeshFilter>();
            if (mf == null) mf = instance.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMesh;

            MeshRenderer mr = instance.GetComponent<MeshRenderer>();
            if (mr == null) mr = instance.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sourceMat;

            // 2. Adjust Transform (Flatten/Scale)
            instance.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // 3. Layer Setup (Item Layer = 10)
            instance.layer = 10; 

            // 4. Rigidbody Setup
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null) rb = instance.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // 5. Collider Setup
            Collider col = instance.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            
            BoxCollider boxCol = instance.AddComponent<BoxCollider>();
            // Auto size box collider
            boxCol.isTrigger = false;

            // 6. Ensure ItemObject script is present
            if (instance.GetComponent<ItemObject>() == null)
            {
                var itemObj = instance.AddComponent<ItemObject>();
                itemObj.itemName = "Wood";
                itemObj.amount = 1;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, woodPrefabPath);
            Debug.Log("<color=green>Successfully upgraded Wood.prefab visuals and physics!</color>");
        }
        finally {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }
}
