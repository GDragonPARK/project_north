using UnityEngine;
using UnityEditor;
using StarterAssets;

public class KnightRigFixer : EditorWindow
{
    [MenuItem("Tools/Fix Knight Rig and Scene")]
    public static void FixRigAndScene()
    {
        string modelPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Characters/Knight.fbx";
        
        // 1. Fix Model Importer
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer != null)
        {
            Debug.Log("Found Knight Model. Updating Rig settings...");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log("Knight Model re-imported with CreateFromThisModel.");
        }
        else
        {
            Debug.LogError($"Could not find ModelImporter at {modelPath}");
            return;
        }

        // 2. Fix Player_New in Scene
        GameObject player = GameObject.Find("Player_New");
        if (player != null)
        {
            Debug.Log("Found Player_New. Applying scene fixes...");

            // Assign new Avatar
            Animator animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                // Load the avatar from the model path
                Avatar newAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(modelPath);
                if (newAvatar != null)
                {
                    animator.avatar = newAvatar;
                    Debug.Log("Assigned new Knight Avatar to Animator.");
                }
                else
                {
                    Debug.LogError("Failed to load Avatar from model path after re-import.");
                }
            }

            // Remove Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
                Debug.Log("Removed Rigidbody from Player_New.");
            }

            // Update ThirdPersonController GroundLayers
            ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
            if (tpc != null)
            {
                tpc.GroundLayers = -1; // Everything
                Debug.Log("Set ThirdPersonController GroundLayers to Everything.");
            }
            
            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("Scene Saved.");
        }
        else
        {
            Debug.LogError("Could not find Player_New in the scene.");
        }
    }
}