using UnityEngine;
using UnityEditor;

public class BoneFinder : EditorWindow
{
    [MenuItem("Tools/Find Hand Bone")]
    public static void FindHand()
    {
        GameObject player = GameObject.Find("Player_New");
        if (player != null)
        {
            Search(player.transform);
        }
    }

    private static void Search(Transform t)
    {
        // Print if name looks like a hand bone
        if (t.name.ToLower().Contains("hand") && t.name.ToLower().Contains("right"))
        {
            Debug.Log($"FOUND CANDIDATE: {GetPath(t)}");
        }
        foreach (Transform child in t) Search(child);
    }

    private static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}