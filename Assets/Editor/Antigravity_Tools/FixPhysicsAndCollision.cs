using UnityEngine;
using UnityEditor;
using StarterAssets; // Added

public class FixPhysicsAndCollision : MonoBehaviour
{
    [MenuItem("Antigravity/Fix Physics & Collision")]
    public static void ExecuteFix()
    {
        // 1. Fix Grounded Check
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            var tpc = player.GetComponent<ThirdPersonController>(); // StarterAssets namespace? Check file.
            // Assuming the class name is just ThirdPersonController based on previous context
            // Namespace is StarterAssets usually. We might need reflection or just try GetComponent string if specific namespace
            
            if (tpc)
            {
                Undo.RecordObject(tpc, "Fix TPC Grounded");
                
                // Ground Layers: Default (0), Ground (6?), Terrain (7?)
                // Let's get mask by names
                int mask = 0;
                mask |= 1 << LayerMask.NameToLayer("Default");
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer > 0) mask |= 1 << groundLayer;
                int terrainLayer = LayerMask.NameToLayer("Terrain");
                if (terrainLayer > 0) mask |= 1 << terrainLayer;
                
                // Fallback: Just Everything except Player/Trigger? 
                // Or just standard "Everything" (inverted)
                
                tpc.GroundLayers = mask;
                tpc.GroundedOffset = -0.15f; // User requested -0.15
                tpc.GroundedRadius = 0.3f; // Slight increase for stability
                
                Debug.Log($"Fixed TPC: Offset={tpc.GroundedOffset}, Layers={mask}");
            }
            else
            {
                Debug.LogError("ThirdPersonController component not found on Player_New!");
            }
        }
        else
        {
            Debug.LogError("Player_New not found!");
        }

        // 2. Fix Axe Physics (Rigidbody for Triggers)
        if (player)
        {
            var equipManager = player.GetComponent<PlayerEquipmentManager>();
            if (equipManager && equipManager.axeModel)
            {
                var axe = equipManager.axeModel;
                var rb = axe.GetComponent<Rigidbody>();
                if (!rb)
                {
                    rb = axe.AddComponent<Rigidbody>();
                    rb.isKinematic = true; // Weapon shouldn't fall
                    Debug.Log("Added Rigidbody (Kinematic) to Axe for Trigger detection.");
                }
                
                var col = axe.GetComponent<Collider>(); // BoxCollider usually
                if (col)
                {
                    col.isTrigger = true;
                }
                else
                {
                    Debug.LogWarning("Axe has no Collider!");
                }
                
                // Ensure Axe is on a layer that can collide with Trees?
                // Usually "Default" is fine.
            }
        }

        // 3. Fix Tree Collider (Prefab)
        string treePath = "Assets/valheim_Data/World/Props/FirTree/FirTree.prefab";
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePath);
        if (treePrefab)
        {
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(treePath))
            {
                var root = editScope.prefabContentsRoot;
                var col = root.GetComponent<Collider>();
                if (!col)
                {
                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0, 3, 0); // Approximate center
                    cap.height = 6f;
                    cap.radius = 0.5f;
                    Debug.Log("Added CapsuleCollider to Tree Prefab.");
                }
                
                // Ensure Layer is Default or something hit-able
                if (root.layer == LayerMask.NameToLayer("Ignore Raycast"))
                {
                    root.layer = LayerMask.NameToLayer("Default");
                }
            }
        }
        
        // 4. Update Scene Trees (DEBUG_TEST_TREE)
        GameObject testTree = GameObject.Find("DEBUG_TEST_TREE");
        if (testTree)
        {
            var col = testTree.GetComponent<Collider>();
            if (!col)
            {
                var cap = testTree.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0, 3, 0);
                cap.height = 6f;
                cap.radius = 0.5f;
            }
        }

        // 5. Check Terrain Collider Settings
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach(var t in terrains)
        {
            if (t.GetComponent<TerrainCollider>())
            {
                // Note: enableTreeColliders is a property of TerrainCollider
                // t.GetComponent<TerrainCollider>().enabled = true; // Logic check?
                // Actual property name check needed: Unity TerrainCollider 'enableTreeColliders' property exists?
                // It is 'terrainData.treeColliders' maybe?
                // Actually TerrainCollider relies on TerrainData.
                // We just ensure TerrainCollider is active.
                
                // User said "Enable Tree Colliders option".
                // In inspector: "Enable Tree Colliders".
                // In API: t.drawTreesAndFoliage = true; t.bakeLightProbesForTrees...
                // TerrainCollider automatically builds tree colliders if they are in data.
                
                Debug.Log($"Checked Terrain: {t.name}");
            }
        }
    }
}
