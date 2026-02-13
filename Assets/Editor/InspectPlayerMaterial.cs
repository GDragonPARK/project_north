using UnityEngine;
using UnityEditor;

public class InspectPlayerMaterial : EditorWindow
{
    [MenuItem("Antigravity/Inspect Player Material")]
    public static void Inspect()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) 
        {
             Debug.LogError("Player_New not found");
             return;
        }

        Renderer[] rends = player.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            if (r.sharedMaterial)
            {
                Debug.Log($"[Material] {r.name} uses '{r.sharedMaterial.name}' with Shader '{r.sharedMaterial.shader.name}'");
                if (r.sharedMaterial.HasProperty("_Cutoff")) Debug.Log($"   Cutoff: {r.sharedMaterial.GetFloat("_Cutoff")}");
                // Check for common transparent issues
                Debug.Log($"   RenderQueue: {r.sharedMaterial.renderQueue}");
            }
            else
            {
                Debug.LogError($"[Material] {r.name} has NO MATERIAL!");
            }
        }
    }
}
