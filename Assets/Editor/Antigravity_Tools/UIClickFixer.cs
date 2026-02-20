using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class UIClickFixer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Fix UI Clicks")]
        public static void FixUIClickIssues()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[UIClickFixer] Operation cancelled by user.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != LoginScenePath)
            {
                if (System.IO.File.Exists(LoginScenePath))
                {
                    EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[UIClickFixer] Login scene not found at: {LoginScenePath}");
                    return;
                }
            }

            var scene = EditorSceneManager.GetActiveScene();

            // 1. Reset EventSystem
            EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in existingSystems)
            {
                Object.DestroyImmediate(es.gameObject);
            }

            GameObject newEventSystemObj = new GameObject("EventSystem");
            EventSystem newEventSystem = newEventSystemObj.AddComponent<EventSystem>();
            newEventSystemObj.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[UIClickFixer] Replaced EventSystem with InputSystemUIInputModule.");

            // 2. Clean Up Raycast Targets
            Canvas canvas = GameObject.Find("Canvas_Login")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
                int countDisabled = 0;
                int countEnabled = 0;

                foreach (Graphic g in graphics)
                {
                    // Check if this graphic is part of a Selectable component
                    bool isInteractive = false;
                    
                    // Is the graphic on the same object as a Selectable?
                    if (g.GetComponent<Selectable>() != null)
                    {
                        isInteractive = true;
                    }
                    // Is the graphic a direct child of a Selectable? (e.g. Text inside Button, Placeholder in InputField)
                    else if (g.transform.parent != null && g.GetComponentInParent<Selectable>() != null)
                    {
                        isInteractive = true;
                    }

                    if (isInteractive)
                    {
                        if (!g.raycastTarget)
                        {
                            g.raycastTarget = true;
                            EditorUtility.SetDirty(g);
                            countEnabled++;
                        }
                    }
                    else
                    {
                        if (g.raycastTarget)
                        {
                            g.raycastTarget = false;
                            EditorUtility.SetDirty(g);
                            countDisabled++;
                        }
                    }
                }
                Debug.Log($"[UIClickFixer] Raycast Target adjustments: Disabled={countDisabled}, Enabled(Protected)={countEnabled}.");
            }
            else
            {
                Debug.LogError("[UIClickFixer] 'Canvas_Login' not found. Cannot adjust RaycastTargets.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[UIClickFixer] UI clicks fix applied successfully.</color>");
            
            Selection.activeGameObject = newEventSystemObj;
        }
    }
}
