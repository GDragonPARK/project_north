using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class UIInteractionFixer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Fix UI Interaction")]
        public static void FixUIInteraction()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[UIInteractionFixer] Operation cancelled by user.");
                return;
            }

            if (!System.IO.File.Exists(LoginScenePath))
            {
                Debug.LogError($"[UIInteractionFixer] Login scene not found at: {LoginScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);

            // 1. Fix EventSystem
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Object.DestroyImmediate(standaloneModule);
                    InputSystemUIInputModule newModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    Debug.Log("[UIInteractionFixer] Replaced StandaloneInputModule with InputSystemUIInputModule.");
                    EditorUtility.SetDirty(eventSystem);
                }
            }

            // 2. Disable Raycast Target for non-interactive elements
            Canvas canvas = GameObject.Find("Canvas_Login")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
                int count = 0;
                foreach (Graphic g in graphics)
                {
                    // Exclude interactive elements
                    if (g.GetComponent<Button>() != null || 
                        g.GetComponent<TMP_InputField>() != null || 
                        g.GetComponentInParent<TMP_InputField>() != null || // text/placeholder inside input field
                        g.GetComponent<ScrollRect>() != null ||
                        g.GetComponent<Scrollbar>() != null)
                    {
                        // Ensure interactive elements DO have raycast target
                        if (!g.raycastTarget)
                        {
                            g.raycastTarget = true;
                            EditorUtility.SetDirty(g);
                            count++;
                        }
                        continue;
                    }

                    // For everything else (backgrounds, titles, status text, logs), disable raycast
                    if (g.raycastTarget)
                    {
                        g.raycastTarget = false;
                        EditorUtility.SetDirty(g);
                        count++;
                    }
                }
                Debug.Log($"[UIInteractionFixer] Adjusted RaycastTarget on {count} UI elements.");
            }
            else
            {
                Debug.LogError("[UIInteractionFixer] 'Canvas_Login' not found.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[UIInteractionFixer] UI Interaction setup completed successfully.</color>");
        }
    }
}
