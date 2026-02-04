using UnityEngine;
using UnityEditor;

public class LayerEnforcer : EditorWindow
{
    [MenuItem("Tools/Enforce Ground Layer")]
    public static void Enforce()
    {
        // 1. Define Layer
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex == -1)
        {
            // Try 8?
            groundLayerIndex = 8;
            Debug.LogWarning("Layer 'Ground' not found by name. Assuming Layer 8.");
        }

        // 2. Find Environment
        GameObject env = GameObject.Find("Environment_Manager");
        if (env)
        {
            env.layer = groundLayerIndex;
            
            // Also set children?
            foreach(Transform t in env.transform)
            {
                t.gameObject.layer = groundLayerIndex;
            }
            
            Debug.Log($"[Enforcer] Set Environment_Manager (and children) to Layer {groundLayerIndex} ({LayerMask.LayerToName(groundLayerIndex)})");
            
            // Verify Collider
            if(env.GetComponent<Collider>()) Debug.Log("[Enforcer] Collider Verified.");
            else Debug.LogError("[Enforcer] NO COLLIDER on Environment!");
        }
        else
        {
            Debug.LogError("[Enforcer] Environment_Manager not found!");
        }

        // 3. Find Player and Update Mask
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc)
            {
                tpc.GroundLayers = (1 << groundLayerIndex);
                Debug.Log($"[Enforcer] Player GroundLayers set to mask: {tpc.GroundLayers.value} (Layer {groundLayerIndex})");
            }
        }
    }
}