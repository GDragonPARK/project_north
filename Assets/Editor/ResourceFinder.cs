using UnityEngine;
using UnityEditor;

public class ResourceFinder : EditorWindow
{
    [MenuItem("Antigravity/🛠️ Find Wood Prefab")]
    public static void FindWood()
    {
        string[] allPrefabs = AssetDatabase.FindAssets("t:GameObject");
        foreach (string guid in allPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null)
            {
                if (go.name.ToLower().Contains("wood") && !path.Contains("Particle") && !path.Contains("Footstep"))
                {
                    Debug.Log($"Found Wood Candidate: {path}");
                }
                
                if (go.GetComponent<ItemObject>() != null)
                {
                    Debug.Log($"Found ItemObject Prefab: {path} - Name: {go.GetComponent<ItemObject>().itemName}");
                }
            }
        }
    }
}
