using UnityEngine;
using UnityEditor;

public class AxeHandSwitcher : EditorWindow
{
    [MenuItem("Tools/Switch Axe to Left Hand")]
    public static void Switch()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) player = GameObject.Find("Player_New");
        
        if (!player)
        {
            Debug.LogError("[Switcher] Player not found (Tag or Name)!");
            return;
        }
        
        Debug.Log($"[Switcher] Analyzing Player: {player.name}");

        Transform axe = null;
        Transform leftHand = null;

        // Recursive DFS
        FindRecursive(player.transform, ref axe, ref leftHand);

        if (axe && leftHand)
        {
            Debug.Log($"[Switcher] Moving {axe.name} -> {leftHand.name}");
            axe.SetParent(leftHand);
            
            // Blade Forward, Top Left, Handle Right in Left Hand
            // Trial values for Left Hand
            axe.localPosition = new Vector3(-0.1f, 0.05f, 0); 
            axe.localEulerAngles = new Vector3(0, -90, 90); 
            Debug.Log($"[Switcher] Success! Axe moved to Left Hand.");
        }
        else
        {
            if(!axe) Debug.LogError("[Switcher] Axe not found!");
            if(!leftHand) Debug.LogError("[Switcher] Left Hand not found!");
        }
    }

    static void FindRecursive(Transform t, ref Transform axe, ref Transform leftHand)
    {
        string name = t.name.ToLower();
        
        // Find Axe
        if (axe == null && (name.Contains("axe_1handed") || (name.Contains("axe") && name.Contains("handed"))))
        {
             axe = t;
        }

        // Find Left Hand
        // Prioritize "hand" and "left" or "hand.l"
        // KayKit uses "Hand.L" often.
        if (leftHand == null)
        {
            bool isHand = name.Contains("hand");
            bool isLeft = name.Contains("left") || name.Contains(".l") || name.EndsWith("_l");
            bool isNotThumb = !name.Contains("thumb") && !name.Contains("index"); // Avoid fingers
            
            if (isHand && isLeft && isNotThumb)
            {
                leftHand = t;
            }
        }

        if (axe != null && leftHand != null) return;

        foreach (Transform child in t)
        {
            FindRecursive(child, ref axe, ref leftHand);
            if (axe != null && leftHand != null) return;
        }
    }
}