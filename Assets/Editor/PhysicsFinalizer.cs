using UnityEngine;
using UnityEditor;
using StarterAssets;
using UnityEditor.SceneManagement;

public class PhysicsFinalizer : EditorWindow
{
    [MenuItem("Tools/Finalize Physics")]
    public static void ApplyFixes()
    {
        // 1. Find Player
        GameObject player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        // 2. Remove Rigidbody (Immediate)
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.Log("Removed Rigidbody from Player_New.");
        }

        // 3. Update ThirdPersonController Settings
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            // User requested explicit values: Offset 0.1, Radius 0.5
            tpc.GroundedOffset = 0.1f; 
            tpc.GroundedRadius = 0.5f;
            Debug.Log("Updated ThirdPersonController: Offset=0.1, Radius=0.5");
        }
        else
        {
            Debug.LogError("ThirdPersonController component missing!");
        }

        // 4. Save Scene
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Scene Saved.");
    }
}