using UnityEngine;
using UnityEditor;
using StarterAssets;

public class PhysicsInspector : EditorWindow
{
    [MenuItem("Tools/Inspect Physics State")]
    public static void Inspect()
    {
        Debug.Log("--- Physics Inspection Start ---");

        // 1. Check Player
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
            if (tpc)
            {
                Debug.Log($"[TPC] GroundLayers value: {tpc.GroundLayers.value} (Binary: {System.Convert.ToString(tpc.GroundLayers.value, 2)})");
                Debug.Log($"[TPC] GroundedOffset: {tpc.GroundedOffset}");
                Debug.Log($"[TPC] GroundedRadius: {tpc.GroundedRadius}");
            }
        }
        else
        {
            Debug.LogError("Player_New not found.");
        }

        // 2. Check Environment
        // Find Terrain by type or name
        Terrain terrain = Terrain.activeTerrain;
        if (terrain)
        {
            GameObject tObj = terrain.gameObject;
            Debug.Log($"[Terrain] Object: {tObj.name}");
            Debug.Log($"[Terrain] Layer: {tObj.layer}");
            
            TerrainCollider tc = tObj.GetComponent<TerrainCollider>();
            if (tc)
            {
                Debug.Log($"[Terrain] TerrainCollider found. Enabled: {tc.enabled}");
            }
            else
            {
                Debug.LogError("[Terrain] TerrainCollider MISSING!");
            }
        }
        else
        {
            Debug.LogWarning("Terrain.activeTerrain is null. Searching for 'Environment_Manager'...");
            GameObject env = GameObject.Find("Environment_Manager");
            if (env)
            {
                Debug.Log($"[Env] Object: {env.name}");
                Debug.Log($"[Env] Layer: {env.layer}");
                var tc = env.GetComponent<TerrainCollider>();
                var mc = env.GetComponent<MeshCollider>();
                if (tc) Debug.Log($"[Env] TerrainCollider found. Enabled: {tc.enabled}");
                else if (mc) Debug.Log($"[Env] MeshCollider found. Enabled: {mc.enabled}");
                else Debug.LogError("[Env] No Collider found on Environment_Manager!");
            }
        }

        Debug.Log("--- Physics Inspection End ---");
    }
}