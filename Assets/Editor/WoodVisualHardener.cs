using UnityEngine;
using UnityEditor;

public class WoodVisualHardener : EditorWindow
{
    [MenuItem("Antigravity/🪵 Harden Wood Visuals & Physics")]
    public static void HardenWood()
    {
        string woodPrefabPath = "Assets/Prefabs/Wood.prefab";
        GameObject woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(woodPrefabPath);

        if (woodPrefab == null)
        {
            Debug.LogError("❌ Critical: 'Ah, Wood.prefab not found at " + woodPrefabPath + "'.");
            return;
        }

        // 1. Load Ghibli Assets (Box.fbx as fallback for Log)
        string meshPath = "Assets/99.ThirdParty/3D set of stylized nature - GHIBLI style/Art/Meshes/Box.fbx";
        GameObject boxSource = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
        
        string matPath = "Assets/99.ThirdParty/3D set of stylized nature - GHIBLI style/Art/Materials/Box.mat";
        Material boxMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (boxSource == null || boxMat == null)
        {
             Debug.LogError($"❌ Asset check failed. Mesh: {boxSource != null}, Mat: {boxMat != null}");
             return;
        }

        Mesh sourceMesh = boxSource.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        
        // Open Prefab Scope
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(woodPrefabPath))
        {
            GameObject root = editScope.prefabContentsRoot;

            // 2. Visual Swap
            MeshFilter mf = root.GetComponent<MeshFilter>();
            if (!mf) mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMesh;

            MeshRenderer mr = root.GetComponent<MeshRenderer>();
            if (!mr) mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = boxMat;
            
            // Adjust Scale for "Log" look (flatten box slightly?)
            root.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            // 3. Layer Hardening (Layer 10 = Item)
            int itemLayer = LayerMask.NameToLayer("Item");
            if (itemLayer == -1) itemLayer = 10; // Fallback
            root.layer = itemLayer;

            // 4. Physics Hardening
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (!rb) rb = root.AddComponent<Rigidbody>();
            rb.mass = 2.0f; // Heavier feel
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // 5. Collider Precision
            // Remove ALL existing colliders to prevent duplicates/mismatches
            foreach (var c in root.GetComponents<Collider>()) DestroyImmediate(c);
            
            BoxCollider col = root.AddComponent<BoxCollider>();
            // Auto-fit is usually default, but let's encourage it
            // Bounds are LOCAL to the mesh, so default center/size usually matches the mesh bounds 
            // unless the mesh pivot is weird. 
            // We can manually encapuslate if needed, but BoxCollider usually defaults to mesh bounds on Add.
            
            // 6. Ensure Script
            if (!root.GetComponent<ItemObject>())
            {
                ItemObject io = root.AddComponent<ItemObject>();
                io.itemName = "Wood";
                io.amount = 1;
            }

            Debug.Log($"<color=cyan><b>[WoodVisualHardener]</b> Prefab Mesh Swapped Successfully (Mesh: {sourceMesh.name})</color>");
            Debug.Log($"<color=cyan><b>[WoodVisualHardener]</b> Interaction Layer Fixed (Layer: {LayerMask.LayerToName(itemLayer)})</color>");
        }
    }
}
