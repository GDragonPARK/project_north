using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class FileLoggerVerify : MonoBehaviour
{
    static FileLoggerVerify()
    {
        EditorApplication.delayCall += () => 
        {
            string logPath = "Assets/Test_Log_Snap.txt";
            File.WriteAllText(logPath, "Starting Snap Verification...\n");
            
            try
            {
                SnapSystemSetup.SetupSnapPrefabs();
                File.AppendAllText(logPath, "SetupSnapPrefabs called.\n");
            }
            catch (System.Exception e)
            {
                File.AppendAllText(logPath, $"SetupSnapPrefabs Error: {e.Message}\n{e.StackTrace}\n");
            }
            
            Verify(logPath);
        };
    }

    public static void Verify(string logPath)
    {
        string[] paths = {
            "Assets/Resources/Building/Recipes/Wood_Floor.asset",
            "Assets/Resources/Building/Recipes/Wood_Wall.asset"
        };

        foreach (var path in paths)
        {
            File.AppendAllText(logPath, $"Checking {path}...\n");
            BuildRecipeSO recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeSO>(path);
            if (recipe == null)
            {
                File.AppendAllText(logPath, $"[FAIL] Recipe missing at {path}\n");
                continue;
            }

            GameObject prefab = recipe.prefab;
            if (prefab == null)
            {
                File.AppendAllText(logPath, $"[FAIL] Prefab missing in recipe {recipe.name}\n");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            
            // 🛡️ Fix: Do not attempt to load FBX or non-prefab files with PrefabUtility
            if (!prefabPath.EndsWith(".prefab"))
            {
                File.AppendAllText(logPath, $"[SKIP] {prefab.name} is not a .prefab file (Path: {prefabPath})\n");
                continue;
            }

            GameObject content = PrefabUtility.LoadPrefabContents(prefabPath);

            BuildingPiece piece = content.GetComponent<BuildingPiece>();
            if (piece == null)
            {
                File.AppendAllText(logPath, $"[FAIL] BuildingPiece component missing on {prefab.name}\n");
            }
            else
            {
                var snaps = content.GetComponentsInChildren<SnapPoint>();
                if (snaps.Length == 0)
                {
                    File.AppendAllText(logPath, $"[FAIL] No SnapPoints found in {prefab.name}\n");
                }
                else
                {
                    File.AppendAllText(logPath, $"[PASS] {prefab.name}: Found {snaps.Length} SnapPoints.\n");
                }
            }
            
            PrefabUtility.UnloadPrefabContents(content);
        }
        AssetDatabase.Refresh();
    }
}
