using UnityEngine;
using UnityEditor;
using StarterAssets;
using System.Collections.Generic;

public class BugFixer : EditorWindow
{
    [MenuItem("Tools/Fix Axe and Clipping")]
    public static void ApplyFixes()
    {
        GameObject player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        // --- 1. Axe Socket Fix ---
        // Goal: Move Weapon_Socket to right hand bone
        // Known path: Rig_Medium/root/hips/spine/chest/upperarm.r/lowerarm.r/wrist.r/hand.r
        // We will search recursively for 'wrist.r' or 'hand.r'
        
        Transform handBone = FindBone(player.transform, "wrist.r"); // Trying wrist first (standard for sockets)
        if (handBone == null) handBone = FindBone(player.transform, "RightHand"); // Fallback
        if (handBone == null) handBone = FindBone(player.transform, "hand.r"); 

        if (handBone != null)
        {
            Transform weaponSocket = FindGameObjectOrChild("Weapon_Socket", player.transform);
            
            if (weaponSocket != null)
            {
                weaponSocket.SetParent(handBone);
                weaponSocket.localPosition = Vector3.zero;
                weaponSocket.localRotation = Quaternion.identity;
                weaponSocket.localScale = Vector3.one;
                
                // Also reset the Axe inside
                if (weaponSocket.childCount > 0)
                {
                    Transform axe = weaponSocket.GetChild(0); // Assuming axe is first child
                    axe.localPosition = Vector3.zero;
                    axe.localRotation = Quaternion.Euler(0, 90, 0); // Adjust rotation if needed, usually 0 or specific
                    // User said "Local Position and Rotation to (0,0,0)". I will respect that.
                    axe.localRotation = Quaternion.identity;
                }
                
                Debug.Log($"[Axe Fix] Parented Weapon_Socket to {handBone.name} and reset transforms.");
            }
            else
            {
                Debug.LogError("[Axe Fix] Weapon_Socket object not found!");
            }
        }
        else
        {
            Debug.LogError("[Axe Fix] Right Hand Bone (wrist.r/hand.r) not found!");
        }

        // --- 2. Clipping Fix ---
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.GroundLayers = -1; // Everything
            tpc.GroundedRadius = 0.35f; // Increased from 0.28
            tpc.GroundedOffset = -0.2f; // Adjusted sensitivity
            Debug.Log($"[Clipping Fix] Updated ThirdPersonController: GroundLayers=Everything, Radius=0.35, Offset=-0.2");
        }
        else
        {
             Debug.LogError("[Clipping Fix] ThirdPersonController component missing!");
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    private static Transform FindBone(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindBone(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindGameObjectOrChild(string name, Transform root)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj.transform;
        
        return FindBone(root, name);
    }
}