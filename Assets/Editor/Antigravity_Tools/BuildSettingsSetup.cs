using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BuildSettingsSetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Build Settings")]
    public static void SetupBuildSettings()
    {
        var scenePaths = new List<string>
        {
            "Assets/1.Scene/LoginScene.unity",
            "Assets/valheim_Data/Scenes/Scenes/main.unity"
        };

        var currentScenes = EditorBuildSettings.scenes.ToList();
        int addedCount = 0;

        foreach (var path in scenePaths)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[BuildSettingsSetup] Scene not found: {path}");
                continue;
            }

            bool alreadyExists = currentScenes.Any(s => s.path == path);
            if (!alreadyExists)
            {
                currentScenes.Add(new EditorBuildSettingsScene(path, true));
                addedCount++;
                Debug.Log($"[BuildSettingsSetup] Added: {path}");
            }
            else
            {
                Debug.Log($"[BuildSettingsSetup] Already registered: {path}");
            }
        }

        EditorBuildSettings.scenes = currentScenes.ToArray();
        Debug.Log($"[BuildSettingsSetup] ✅ Build Settings updated. {addedCount} scene(s) added. Total: {currentScenes.Count}");
    }
}
