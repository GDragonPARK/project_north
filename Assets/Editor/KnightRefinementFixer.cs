using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

public class KnightRefinementFixer : EditorWindow
{
    [MenuItem("Tools/Fix Knight Camera and Weapon")]
    public static void FixCameraAndWeapon()
    {
        GameObject player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        // 1. Camera Fix
        Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
        if (cameraRoot != null)
        {
            cameraRoot.localPosition = new Vector3(0, 1.5f, 0);
            cameraRoot.localRotation = Quaternion.identity;
            Debug.Log("Fixed PlayerCameraRoot position and rotation.");
        }
        else
        {
            Debug.LogError("PlayerCameraRoot not found under Player_New!");
        }

        // 2. Weapon Socket Fix
        Transform handBone = FindDeepChild(player.transform, "wrist.r"); // Found via traversal
        if (handBone != null)
        {
            // Find existing socket anywhere, or create it? User implies it exists but is wrong.
            // Search globally or under player? User says "Weapon_Socket is made".
            // Let's look for it under Player or global.
            GameObject socket = GameObject.Find("Weapon_Socket");
            
            // If not found global, check deep child
            if (socket == null)
            {
                Transform socketTr = FindDeepChild(player.transform, "Weapon_Socket");
                if (socketTr != null) socket = socketTr.gameObject;
            }

            if (socket != null)
            {
                socket.transform.SetParent(handBone);
                socket.transform.localPosition = Vector3.zero;
                socket.transform.localRotation = Quaternion.identity;
                Debug.Log($"Reparented Weapon_Socket to {handBone.name}.");

                // Reset Axe inside
                Transform axe = FindDeepChild(socket.transform, "axe_1handed"); // Heuristic name check
                if (axe == null) axe = socket.transform.GetChild(0); // Assuming it's the first child if name differs

                if (axe != null)
                {
                    axe.localPosition = Vector3.zero;
                    axe.localRotation = Quaternion.identity;
                    Debug.Log("Reset Axe position inside Socket.");
                }
            }
            else
            {
                Debug.LogError("Weapon_Socket object not found to reparent!");
            }
        }
        else
        {
            Debug.LogError("Knight_HandRight bone not found! Check hierarchy names.");
        }

        // 3. Input Fix
        PlayerInput input = player.GetComponent<PlayerInput>();
        if (input != null)
        {
            input.enabled = true;
            Debug.Log("Ensured PlayerInput component is Enabled.");
        }
        else
        {
             Debug.LogError("PlayerInput component missing on Player_New!");
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}