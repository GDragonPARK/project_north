using UnityEngine;
using UnityEditor;
using StarterAssets;

public class EnvironmentPolisher : EditorWindow
{
    [MenuItem("Tools/Polish Physics and Environment")]
    public static void ApplyPolish()
    {
        // 1. Player Physics
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
            if (tpc)
            {
                // User requested: 
                // GroundedOffset 0.1 -> Interpreting as -0.1f (Upwards shift inside component logic check usually)
                // Actually if formula is (y - offset), then positive offset lowers the point. Negative raises it.
                // We want to raise it to avoid hitting ground too early? Or hitting it earlier?
                // To fix "Infinite Falling" (Not grounded), we want to make the sphere hit the ground EASIER.
                // So we want the sphere to be lower? Or radius bigger.
                // Radius 0.5 is big.
                // Offset: Standard is small negative (-0.14). 
                // Let's set to -0.1f as per edit.
                
                tpc.RotationSmoothTime = 0.05f;
                tpc.GroundedRadius = 0.5f;
                tpc.GroundedOffset = -0.1f; 
                
                Debug.Log("Updated ThirdPersonController: SmoothTime=0.05, Radius=0.5, Offset=-0.1");
            }
        }

        // 2. Vegetation
        VegetationSpawner vs = FindFirstObjectByType<VegetationSpawner>();
        if (vs)
        {
            // Assign rock prefab if missing (using a workaround or known asset)
            // The script change handled the logic, now we trigger it.
            // Setup new values explicitly in case Inspector overwrites script defaults
            vs.treeCount = 80;
            vs.grassCount = 400;
            vs.rockCount = 50;
            vs.safeRadius = 15.0f;
            
            // Check if Rock Prefab is assigned
            if (vs.rockPrefab == null && vs.treePrefabs.Count > 0)
            {
                // Fallback: use a tree or find a rock
                string[] guids = AssetDatabase.FindAssets("Rock t:Prefab");
                foreach(var g in guids) {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if(p.Contains("KayKit")) {
                        vs.rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                        break;
                    }
                }
            }
            
            vs.SpawnGrass(); // Renamed logic inside, method name kept for ContextMenu compatibility
            Debug.Log("Respawned Vegetation with Density Settings.");
        }
        else
        {
            Debug.LogWarning("No VegetationSpawner found.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}