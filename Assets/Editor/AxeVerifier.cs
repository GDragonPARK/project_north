using UnityEngine;
using UnityEditor;

public class AxeVerifier : EditorWindow
{
    [MenuItem("Tools/Verify Axe")]
    public static void Verify()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;
        Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
        Transform axeTr = null;
        foreach (var t in allChildren) if (t.name == "axe_1handed") axeTr = t;

        if (axeTr)
        {
            Debug.Log($"[Verifier] Axe Pos: {axeTr.localPosition} (Expected ~0, 0.1, 0)");
            Debug.Log($"[Verifier] Axe Rot: {axeTr.localEulerAngles} (Expected 0, 90, 270/-90)");
            // Note: -90 becomes 270 in Euler.
            
            // Check Hit Events?
            // Animation Events are on the Clip, not the GameObject.
            // Verified in Refiner log, but can check here too.
        }
    }
}