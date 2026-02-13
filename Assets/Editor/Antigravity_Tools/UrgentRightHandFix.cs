using UnityEngine;
using UnityEditor;

public class UrgentRightHandFix : MonoBehaviour
{
    [MenuItem("Antigravity/URGENT Right Hand Fix")]
    public static void ExecuteFix()
    {
        // 1. Locate Player_New
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player_New not found! Cannot proceed.");
            return;
        }

        // 2. Locate PlayerEquipmentManager
        PlayerEquipmentManager equipment = player.GetComponent<PlayerEquipmentManager>();
        if (!equipment || !equipment.axeModel)
        {
            Debug.LogError("PlayerEquipmentManager or Axe Model missing on Player_New!");
            return;
        }

        GameObject axe = equipment.axeModel;
        
        // 3. Find Right Hand Bone (Try all naming conventions)
        Transform rightHand = FindDeepChild(player.transform, "RightHand"); // Standard
        if (!rightHand) rightHand = FindDeepChild(player.transform, "hand.r"); // KayKit/Mixamo
        if (!rightHand) rightHand = FindDeepChild(player.transform, "Hand.R");
        if (!rightHand) rightHand = FindDeepChild(player.transform, "R_Hand");
        if (!rightHand) rightHand = FindDeepChild(player.transform, "RightHandAnchor"); 

        if (!rightHand)
        {
             // Debug dump again if still failing
             Debug.LogError("CRITICAL: Right Hand bone NOT found. Dumping hierarchy...");
             PrintHierarchy(player.transform);
             return;
        }

        Undo.RecordObject(axe.transform, "Right Hand Socket Fix");

        // 4. Force Reparent to Right Hand
        if (axe.transform.parent != rightHand)
        {
            axe.transform.SetParent(rightHand);
            Debug.Log($"[Fix] Axe moved to Right Hand: {rightHand.name}");
        }

        // 5. Reset Transform (Zero out)
        axe.transform.localPosition = Vector3.zero;
        axe.transform.localRotation = Quaternion.identity;
        axe.transform.localScale = Vector3.one;

        // 6. Apply Corrective Rotation (Trial 1: -90 Y?)
        // User says "Blade still flipped". 
        // If Model Z is forward (blade edge), and Hand Z is forward (thumb? or out?).
        // Usually: Hand Z is Out or Up. 
        // Let's try rotating to align blade Outward.
        // Common fix: Rotation (0, 90, -90) or (0, -90, 0).
        // Let's try 90 on Y first (Right angle).
        
        // User said: "It was left hand, now right hand needed. Blade was backward."
        // We moved to right hand. Now we need to orient.
        // Let's set a standard "Grip" rotation.
        // Typically axes need -90 rotation on one axis to be perpendicular to the hand.
        // Let's try: Position (0, 0, 0), Rotation (0, 90, 90) or similar.
        // Let's start with (0,0,0) and let user adjust if needed, OR apply a "Smart Guess"
        // Guess: (0, -180, -90) might be it if it was upside down.
        
        // Let's just reset to Identity (0,0,0) as a baseline.
        // User said "Blade backward". If identity is backward, we need 180 Y.
        // If we previously did 180 Y and it was still wrong/backward in LEFT hand... now in RIGHT hand it might be different.
        // Let's set to (0,0,0) first, then apply a -90 X rotation (often needed for weapons to point forward).
        
        // Let's try (0, 0, 0) for Position, and maybe (0, 180, 0) for Rotation as a safest bet if it was facing self.
        // But wait, user said "Blade orientation backward".
        // Let's try rotating Y by 180.
        axe.transform.localRotation = Quaternion.Euler(0, 180, 0); 
        
        // Also check if Position needs offset (often palm center is not pivot).
        // Let's effectively zero it for now.
        
        Debug.Log($"[Fix] Axe Re-Socketed to {rightHand.name} & Rotated (0, 180, 0).");

        // Save
        EditorUtility.SetDirty(player);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child; // Case insensitive
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static void PrintHierarchy(Transform t)
    {
        Debug.Log(t.name);
        foreach(Transform child in t) PrintHierarchy(child);
    }
}
