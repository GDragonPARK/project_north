using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class VerifySnapSetup : MonoBehaviour
{
    static VerifySnapSetup()
    {
        EditorApplication.delayCall += () => 
        {
            SnapSystemSetup.SetupSnapPrefabs();
            Verify();
        };
    }

    [MenuItem("Tools/Building System/Verify Snap Setup")]
    public static void Verify()
    {
        string[] paths = {
            "Assets/Resources/Building/Recipes/Wood_Floor.asset",
            "Assets/Resources/Building/Recipes/Wood_Wall.asset"
        };

        foreach (var path in paths)
        {
            BuildRecipeSO recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeSO>(path);
            if (recipe == null)
            {
                Debug.LogError($"Recipe missing at {path}");
                continue;
            }

            GameObject prefab = recipe.prefab;
            if (prefab == null)
            {
                Debug.LogError($"Prefab missing in recipe {recipe.name}");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (!prefabPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"[VerifySnapSetup] {prefab.name} is not a .prefab file. Skipping.");
                continue;
            }

            GameObject content = PrefabUtility.LoadPrefabContents(prefabPath);

            BuildingPiece piece = content.GetComponent<BuildingPiece>();
            if (piece == null)
            {
                Debug.LogError($"[FAIL] BuildingPiece component missing on {prefab.name}");
            }
            else
            {
                // Must call CacheSnapPoints to populate list if empty? 
                // Creating script execution context might be tricky without instance.
                // Just check children.
                var snaps = content.GetComponentsInChildren<SnapPoint>();
                if (snaps.Length == 0)
                {
                    Debug.LogError($"[FAIL] No SnapPoints found in {prefab.name}");
                }
                else
                {
                    Debug.Log($"[PASS] {prefab.name}: Found BuildingPiece and {snaps.Length} SnapPoints.");
                    foreach(var sp in snaps)
                    {
                         // Layer check
                         if (sp.gameObject.layer != LayerMask.NameToLayer("SnapPoint"))
                            Debug.LogWarning($"[WARN] SnapPoint {sp.name} layer is {LayerMask.LayerToName(sp.gameObject.layer)}, expected SnapPoint");
                    }
                }
            }
            
            PrefabUtility.UnloadPrefabContents(content);
        }
    }
}
