using UnityEngine;
using UnityEditor;
using StarterAssets;
using System.Collections.Generic;

public class FixPlayerAttributes : MonoBehaviour
{
    [MenuItem("Antigravity/Fix Player Attributes & Axe")]
    public static void FixedPlayer()
    {
        // 1. Find Player
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        Undo.RecordObject(player, "Fix Player Attributes");

        // 2. Fix ThirdPersonController (Grounding)
        var tpc = player.GetComponent<ThirdPersonController>();
        if (tpc)
        {
            Undo.RecordObject(tpc, "Fix TPC Grounding");
            
            // Layer Fix: Default(0) | Ground(6) | Terrain(Something else?)
            // Usually Terrain is layer 6 or Default. Let's make sure it hits Everything except Player/Trigger? 
            // Or just Default + Ground + Terrain(if exists)
            // Let's set it to Default(1) + Layer 6(if exists) + Layer 7(if exists) + ... 
            // Better: Get mask by name.
            int mask = LayerMask.GetMask("Default", "Ground", "Terrain");
            if (mask == 0) mask = 1; // Fallback to Default
            
            tpc.GroundLayers = mask;
            tpc.GroundedOffset = -0.15f; // Lower offset to catch ground better
            tpc.GroundedRadius = 0.28f; // Standardize radius 
            
            Debug.Log($"[Fix] Updated ThirdPersonController: Offset={tpc.GroundedOffset}, Layers={mask}");
        }

        // 3. Fix Axe Socket
        var equipment = player.GetComponent<PlayerEquipmentManager>();
        if (equipment && equipment.axeModel)
        {
            GameObject axe = equipment.axeModel;
            Transform rightHand = FindDeepChild(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "hand.r"); // KayKit/Mixamo (lowercase)
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "hand.R");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "Hand.R");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "R_Hand");
            
            if (rightHand)
            {
                if (axe.transform.parent != rightHand)
                {
                    Undo.SetTransformParent(axe.transform, rightHand, "Reparent Axe");
                    Debug.Log($"[Fix] Reparented Axe to {rightHand.name}");
                }
                
                Undo.RecordObject(axe.transform, "Reset Axe Transform");
                axe.transform.localPosition = Vector3.zero;
                // KayKit hand needs specific rotation usually. Let's try identity first as requested, 
                // but usually weapons need -90 or 90 rotation. User asked for 0,0,0.
                axe.transform.localRotation = Quaternion.identity; 
                axe.transform.localScale = Vector3.one;
                
                Debug.Log($"[Fix] Reset Axe Transform to (0,0,0) relative to {rightHand.name}.");
            }
            else
            {
                Debug.LogError("Could not find 'RightHand' (or hand.r) bone in Player hierarchy!");
                Debug.Log("Dumping child names for debug:");
                PrintHeirarchy(player.transform);
            }
        }
        else
        {
             Debug.LogWarning("PlayerEquipmentManager or Axe Model missing.");
        }

        // 4. Save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    private static void PrintHeirarchy(Transform t)
    {
         Debug.Log(t.name);
         foreach(Transform child in t) PrintHeirarchy(child);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name) || child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
