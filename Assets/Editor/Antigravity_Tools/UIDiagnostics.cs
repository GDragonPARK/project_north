using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Antigravity.Tools
{
    public class UIDiagnostics : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Run UI Diagnostics")]
        public static void RunDiagnostics()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (EditorSceneManager.GetActiveScene().path != LoginScenePath)
            {
                if (System.IO.File.Exists(LoginScenePath))
                {
                    EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[UIDiagnostics] Login scene not found at: {LoginScenePath}");
                    return;
                }
            }
            
            var scene = EditorSceneManager.GetActiveScene();
            bool modificationsMade = false;

            // A) Canvas GraphicRaycaster Check
            Canvas canvas = GameObject.Find("Canvas_Login")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.LogWarning("[UIDiagnostics] <color=orange>GraphicRaycaster was missing on Canvas_Login.</color> Attached it.");
                    modificationsMade = true;
                }
                else
                {
                    Debug.Log("[UIDiagnostics] GraphicRaycaster is present on Canvas_Login.");
                }

                // D) CanvasGroup Inspection
                CanvasGroup[] cgArray = canvas.GetComponentsInChildren<CanvasGroup>(true);
                foreach (var cg in cgArray)
                {
                    bool changed = false;
                    if (!cg.blocksRaycasts)
                    {
                        cg.blocksRaycasts = true;
                        changed = true;
                    }
                    if (!cg.interactable)
                    {
                        cg.interactable = true;
                        changed = true;
                    }
                    if (changed)
                    {
                        EditorUtility.SetDirty(cg);
                        modificationsMade = true;
                        Debug.LogWarning($"[UIDiagnostics] <color=orange>CanvasGroup on {cg.gameObject.name} was blocking interaction.</color> Fixed.");
                    }
                }
            }
            else
            {
                Debug.LogError("[UIDiagnostics] Canvas_Login not found!");
            }

            // B) EventSystem Check
            EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem targetEventSystem = null;

            if (existingSystems.Length == 0)
            {
                GameObject newEventSystemObj = new GameObject("EventSystem");
                targetEventSystem = newEventSystemObj.AddComponent<EventSystem>();
                newEventSystemObj.AddComponent<InputSystemUIInputModule>();
                Debug.LogWarning("[UIDiagnostics] <color=orange>No EventSystem found.</color> Created new EventSystem with InputSystemUIInputModule.");
                modificationsMade = true;
            }
            else
            {
                targetEventSystem = existingSystems[0];
                if (existingSystems.Length > 1)
                {
                    Debug.LogWarning($"[UIDiagnostics] <color=orange>Found {existingSystems.Length} EventSystems.</color> Destroying duplicates.");
                    for (int i = 1; i < existingSystems.Length; i++)
                    {
                        Object.DestroyImmediate(existingSystems[i].gameObject);
                    }
                    modificationsMade = true;
                }

                if (targetEventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    StandaloneInputModule standalone = targetEventSystem.GetComponent<StandaloneInputModule>();
                    if (standalone != null) Object.DestroyImmediate(standalone);

                    targetEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    Debug.LogWarning("[UIDiagnostics] <color=orange>InputSystemUIInputModule was missing.</color> Attached it to EventSystem.");
                    modificationsMade = true;
                }
                else
                {
                    Debug.Log("[UIDiagnostics] EventSystem and Input Module are correct.");
                }
            }

            // C & E) ConnectButton Check
            LoginUIController controller = Object.FindAnyObjectByType<LoginUIController>();
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty connectBtnProp = so.FindProperty("connectButton");
                if (connectBtnProp != null && connectBtnProp.objectReferenceValue != null)
                {
                    Button connectBtn = connectBtnProp.objectReferenceValue as Button;
                    if (connectBtn != null)
                    {
                        // C) RaycastTarget Check
                        if (connectBtn.targetGraphic != null && !connectBtn.targetGraphic.raycastTarget)
                        {
                            connectBtn.targetGraphic.raycastTarget = true;
                            EditorUtility.SetDirty(connectBtn.targetGraphic);
                            Debug.LogWarning("[UIDiagnostics] <color=orange>ConnectButton targetGraphic raycastTarget was FALSE.</color> Forced it to TRUE.");
                            modificationsMade = true;
                        }

                        // E) UIHoverFeedback Check
                        if (connectBtn.GetComponent<UIHoverFeedback>() == null)
                        {
                            connectBtn.gameObject.AddComponent<UIHoverFeedback>();
                            Debug.LogWarning("[UIDiagnostics] <color=orange>UIHoverFeedback was missing from ConnectButton.</color> Attached it.");
                            EditorUtility.SetDirty(connectBtn.gameObject);
                            modificationsMade = true;
                        }
                    }
                }
            }

            if (modificationsMade)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("<color=green>UI 진단 및 복구 완료</color>");
        }
    }
}
