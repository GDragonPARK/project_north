using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using Cinemachine;
using StarterAssets;
using System.IO;

public class ConsolidatedFixer : EditorWindow
{
    [MenuItem("Tools/Finalize Player and Cleanup")]
    public static void RunFixes()
    {
        GameObject player = GameObject.Find("Player_New");
        if (player == null) { Debug.LogError("Player_New not found"); return; }

        // 1. Input Action Fix
        PlayerInput input = player.GetComponent<PlayerInput>();
        if (input != null)
        {
            // Load the specific asset
            string actionPath = "Assets/99.ThirdParty/StarterAssets/InputSystem/StarterAssets.inputactions";
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionPath);
            if (actions != null)
            {
                input.actions = actions;
                input.defaultActionMap = "Player";
                input.enabled = false; // Toggle to refresh
                input.enabled = true;
                Debug.Log($"Assigned Input Actions from {actionPath}");
            }
            else
            {
                Debug.LogError($"Could not find InputActionAsset at {actionPath}");
            }
        }

        // 2. Camera Fix
        Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.identity;
            cameraRoot.localPosition = new Vector3(0, 1.5f, 0); // Ensure height
            Debug.Log("Reset PlayerCameraRoot rotation/position.");

            // Link Cinemachine
            GameObject cmObj = GameObject.Find("CM FreeLook1");
            if (cmObj != null)
            {
                CinemachineFreeLook cm = cmObj.GetComponent<CinemachineFreeLook>();
                if (cm != null)
                {
                    cm.Follow = cameraRoot;
                    cm.LookAt = cameraRoot;
                    Debug.Log("Linked CM FreeLook1 to PlayerCameraRoot.");
                }
            }
            else
            {
                 Debug.LogWarning("CM FreeLook1 not found.");
            }
        }

        // 3. Ground Layers Fix
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.GroundLayers = -1; // Everything
            Debug.Log("Set GroundLayers to Everything.");
        }

        // 4. File Cleanup
        string[] filesToMove = { "Assets/New Terrain.asset", "Assets/New Terrain 1.asset" };
        string destFolder = "Assets/2.Model/Environment";
        
        if (!AssetDatabase.IsValidFolder(destFolder))
        {
            AssetDatabase.CreateFolder("Assets/2.Model", "Environment");
        }

        foreach (string file in filesToMove)
        {
            if (File.Exists(file) || AssetDatabase.LoadAssetAtPath<Object>(file) != null)
            {
                string fileName = Path.GetFileName(file);
                string err = AssetDatabase.MoveAsset(file, $"{destFolder}/{fileName}");
                if (string.IsNullOrEmpty(err)) Debug.Log($"Moved {fileName} to Environment folder.");
                else Debug.LogWarning($"Failed to move {fileName}: {err}");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}