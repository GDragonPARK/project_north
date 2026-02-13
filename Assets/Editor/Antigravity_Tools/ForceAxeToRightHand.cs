using UnityEngine;
using UnityEditor;

public class ForceAxeToRightHand : MonoBehaviour
{
    [MenuItem("Antigravity/URGENT Force Axe Right")]
    public static void ForceMove()
    {
        // 1. Find Player
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;

        // 2. Find Hands
        Transform rightHand = FindBone(player.transform, "hand.r", "RightHand", "Hand.R");
        Transform leftHand = FindBone(player.transform, "hand.l", "LeftHand", "Hand.L");

        if (!rightHand) 
        {
            Debug.LogError("CRITICAL: No Right Hand Bone found!");
            return;
        }

        // 3. Find ALL Axes in Player Children (to kill duplicates)
        // Look for typical names: "Axe", "axe_1handed", "Axe_Model"
        // Collect them
        var allDescendants = player.GetComponentsInChildren<Transform>(true);
        var axes = new System.Collections.Generic.List<Transform>();
        
        foreach(var t in allDescendants)
        {
             if (t == null) continue;
             if (t.name.ToLower().Contains("axe") || t.name.ToLower().Contains("weapon"))
             {
                 axes.Add(t);
             }
        }

        Debug.Log($"Found {axes.Count} axe-like objects.");

        // Clean duplicates first
        // We need to be careful not to destroy the MAIN one.
        // Identify Main One via Manager
        var mgr = player.GetComponent<PlayerEquipmentManager>();
        GameObject mainAxeObj = (mgr && mgr.axeModel) ? mgr.axeModel : null;
        Transform mainAxe = mainAxeObj ? mainAxeObj.transform : null;

        if (!mainAxe)
        {
            Debug.LogError("Manager has no axeModel assigned! Cannot determine main axe.");
            return;
        }

        // Iterate backwards to allow destruction
        for (int i = axes.Count - 1; i >= 0; i--)
        {
            var axe = axes[i];
            if (!axe) continue; // Already destroyed?

            if (axe == mainAxe)
            {
                Debug.Log($"Identified Linked Main Axe: {axe.name}");
                continue; // Keep this one
            }
            
            // It's not the main one. Is it in the wrong hand?
            if (IsChildOf(axe, leftHand))
            {
                 Debug.LogWarning($"Found Duplicate Axe in Left Hand: {axe.name} -> DESTROYING");
                 Undo.DestroyObjectImmediate(axe.gameObject);
            }
            else if (IsChildOf(axe, rightHand))
            {
                 Debug.LogWarning($"Found Extra Axe in Right Hand: {axe.name} -> Checking if duplicate...");
                 // If not main, destroy it?
                 if (axe != mainAxe)
                 {
                     Undo.DestroyObjectImmediate(axe.gameObject);
                 }
            }
        }

        // 4. Force Move Main Axe
        if (mainAxe)
        {
            if (mainAxe.parent != rightHand)
            {
                Undo.SetTransformParent(mainAxe, rightHand, "Move Axe to Right");
                Debug.Log("MOVED Main Axe to Right Hand.");
            }
            
            // Reset Transforms
            // User: "Blade still backward". 
            // Previous attempt: (0, 180, 0).
            // Let's try visual guess based on new parent.
            // Safe bet: Identity first. Then user can tweak.
            // BUT, if it KEEPS snapping back on Play... it might be Animated!
            
            mainAxe.localPosition = Vector3.zero;
            mainAxe.localRotation = Quaternion.Euler(0, 180, 0); // Keep the 180 fix if it helped orientation
             
            Debug.Log("Reset Main Axe Transform.");
        }
        else
        {
            Debug.LogError("No Main Axe found linked in Manager!");
        }

        EditorUtility.SetDirty(player);
    }

    private static Transform FindBone(Transform t, params string[] names)
    {
        foreach(var n in names)
        {
            if (t.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return t;
        }
        foreach(Transform child in t)
        {
            var res = FindBone(child, names);
            if (res) return res;
        }
        return null;
    }

    private static void FindAxesRecursive(Transform t, System.Collections.Generic.List<Transform> list)
    {
        if (t.name.ToLower().Contains("axe") || t.name.ToLower().Contains("weapon"))
        {
            list.Add(t);
        }
        foreach(Transform child in t) FindAxesRecursive(child, list);
    }

    private static bool IsChildOf(Transform child, Transform parent)
    {
        if (!parent) return false;
        var p = child.parent;
        while (p != null)
        {
            if (p == parent) return true;
            p = p.parent;
        }
        return false;
    }
}
