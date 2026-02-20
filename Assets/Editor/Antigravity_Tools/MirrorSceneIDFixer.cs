using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;

namespace Antigravity.Tools
{
    public class MirrorSceneIDFixer : EditorWindow
    {
        [MenuItem("Tools/Project North/Fix Mirror Scene IDs")]
        public static void FixSceneIDs()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string[] scenesToFix = new string[] 
            { 
                "Assets/1.Scene/Main.unity", 
                "Assets/1.Scene/LoginScene.unity" 
            };

            foreach (string scenePath in scenesToFix)
            {
                if (System.IO.File.Exists(scenePath))
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    
                    // Force all NetworkIdentities to generate scene ID
                    NetworkIdentity[] identities = Object.FindObjectsOfType<NetworkIdentity>(true);
                    foreach(var id in identities)
                    {
                        // Access private method or just changing value triggers onvalidate
                        EditorUtility.SetDirty(id);
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[MirrorSceneIDFixer] Re-saved scene to fix NetworkIdentity IDs: {scenePath}");
                }
            }

            Debug.Log("<color=green>[MirrorSceneIDFixer] All specified scenes fixed and saved.</color>");
        }
    }
}
