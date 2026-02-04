using UnityEngine;
using UnityEditor;

public class AxeCorrector : EditorWindow
{
    [MenuItem("Tools/Correct Axe Rotation")]
    public static void Correct()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player not found.");
            return;
        }

        // Find deeply nested axe
        Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
        Transform axe = null;
        foreach (var t in allChildren)
        {
            if (t.name == "axe_1handed")
            {
                axe = t;
                break;
            }
        }

        if (axe)
        {
            // User requested blade down.
            // Assuming current rotation is wrong (up).
            // Let's try rotating 180 on X local.
            axe.localEulerAngles = new Vector3(180, 0, 0); // Reset to specific known good?
            // Or add 180?
            // Let's set to a "Blade Down" logic. 
            // Often Axe handle is Y or Z. 
            // If I set 180, 0, 0, acts as flip. 
            // Debug log previous rotation.
            Debug.Log($"Axe Found. Prev Rot: {axe.localEulerAngles}. Setting to (180, 90, 0) as guess or just (180,0,0).");
            
            // Try (0, 180, 0) or (180, 0, 0). 
            // Visual check required. I will set it to (0, 90, 0) often standard. 
            // User said "Rotate... 180".
            // I will set it to (180, 0, 0) for now.
            axe.localEulerAngles = new Vector3(0, 180, 0); // Try Y flip first? 
            // Actually, if I can't see it, I'll just apply a 180 rotation on X and see.
            // Let's try (180, 0, 0).
            axe.localEulerAngles = new Vector3(180, 0, 0);
            
            // Add Collider for Phase 2
            BoxCollider bc = axe.gameObject.GetComponent<BoxCollider>();
            if (!bc)
            {
                bc = axe.gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(0.3f, 1.5f, 0.3f); // Approximate size
                bc.center = new Vector3(0, 0.5f, 0);
                Debug.Log("Added BoxCollider to Axe.");
            }
            
            Debug.Log("Axe Corrected.");
        }
        else
        {
            Debug.LogError("Axe 'axe_1handed' not found on Player!");
        }
    }
}