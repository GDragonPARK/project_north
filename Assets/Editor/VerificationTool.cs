using UnityEngine;
using UnityEditor;

public class VerificationTool
{
    [MenuItem("Tools/Verify Scene")]
    public static void Verify()
    {
        // 1. Verify Player & Knight
        GameObject player = GameObject.Find("Player") ?? GameObject.Find("PlayerArmature");
        if (player)
        {
            Debug.Log($"VERIFY: Found Player object: {player.name}");
            var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(player);
            Debug.Log($"VERIFY: Prefab Status: {prefabStatus}");
            if (prefabStatus == PrefabInstanceStatus.Connected)
            {
                Object parentObject = PrefabUtility.GetCorrespondingObjectFromSource(player);
                string path = AssetDatabase.GetAssetPath(parentObject);
                Debug.Log($"VERIFY: Prefab Source Path: {path}");
            }

            Debug.Log("VERIFY: Player Children:");
            foreach(Transform child in player.transform) Debug.Log($" - {child.name}");

            Transform visuals = player.transform.Find("Knight_Visuals");
            if (visuals) 
            {
                 Debug.Log("VERIFY: Knight_Visuals FOUND.");
                 // ... socket check ...
            }
            else Debug.LogError("VERIFY: Knight_Visuals NOT FOUND under Player.");
        }
        else Debug.LogError("VERIFY: Player Root NOT FOUND.");

        // 3. Verify Terrain
        Terrain t = Object.FindFirstObjectByType<Terrain>();
        if (t)
        {
            Debug.Log($"VERIFY: Terrain Resolution: {t.terrainData.heightmapResolution}");
            if (t.terrainData.heightmapResolution == 513) Debug.Log("VERIFY: Terrain Resolution MATCHES 513.");
            else Debug.LogError($"VERIFY: Terrain Resolution MISMATCH: {t.terrainData.heightmapResolution}.");
        }
        else Debug.LogError("VERIFY: Terrain NOT FOUND.");
        
        Debug.Log("VERIFICATION COMPLETE.");
    }

    private static Transform FindDeepChild(Transform aParent, string aName)
    {
        foreach(Transform child in aParent)
        {
            if(child.name == aName) return child;
            var result = FindDeepChild(child, aName);
            if (result != null) return result;
        }
        return null;
    }
}