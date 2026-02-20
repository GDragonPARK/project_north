using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class UltimateUIFixer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Ultimate UI Fix")]
        public static void ExecuteUltimateFix()
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
                    Debug.LogError($"[UltimateUIFixer] Login scene not found at: {LoginScenePath}");
                    return;
                }
            }
            
            var scene = EditorSceneManager.GetActiveScene();

            // 1. Inject Hover Feedback to Connect Button
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
                        if (connectBtn.GetComponent<UIHoverFeedback>() == null)
                        {
                            connectBtn.gameObject.AddComponent<UIHoverFeedback>();
                            Debug.Log("[UltimateUIFixer] Added UIHoverFeedback to ConnectButton.");
                            EditorUtility.SetDirty(connectBtn.gameObject);
                        }
                    }
                }
            }

            // 2. Global Raycast Purge & Selective Restoration
            Canvas canvas = GameObject.Find("Canvas_Login")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Ensure Canvas has a GraphicRaycaster
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("[UltimateUIFixer] Added missing GraphicRaycaster to Canvas_Login.");
                }

                // A) Aggressively disable ALL Graphic raycasts
                Graphic[] allGraphics = canvas.GetComponentsInChildren<Graphic>(true);
                foreach (var g in allGraphics)
                {
                    g.raycastTarget = false;
                    EditorUtility.SetDirty(g);
                }
                Debug.Log($"[UltimateUIFixer] Purged raycastTarget on {allGraphics.Length} UI elements.");

                // B & C) Restore ONLY for Selectables
                Selectable[] selectables = canvas.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    // The core interaction area
                    if (selectable.targetGraphic != null)
                    {
                        selectable.targetGraphic.raycastTarget = true;
                        EditorUtility.SetDirty(selectable.targetGraphic);
                    }

                    // Special rules for InputField (we need to click the text/placeholder area as well)
                    if (selectable is TMP_InputField inputField)
                    {
                        if (inputField.textComponent != null) 
                        {
                            inputField.textComponent.raycastTarget = true;
                            EditorUtility.SetDirty(inputField.textComponent);
                        }
                        
                        var placeholder = inputField.placeholder as Graphic;
                        if (placeholder != null)
                        {
                            placeholder.raycastTarget = true;
                            EditorUtility.SetDirty(placeholder);
                        }
                    }
                }
                Debug.Log($"[UltimateUIFixer] Protected raycastTarget for {selectables.Length} Selectable elements.");
            }
            else
            {
                Debug.LogError("[UltimateUIFixer] Canvas_Login not found!");
            }

            // 3. EventSystem Validation
            EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in existingSystems)
            {
                Object.DestroyImmediate(es.gameObject);
            }

            GameObject newEventSystemObj = new GameObject("EventSystem");
            newEventSystemObj.AddComponent<EventSystem>();
            newEventSystemObj.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[UltimateUIFixer] Reset EventSystem for New Input System compatibility.");

            // Finalize
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = newEventSystemObj;
            
            Debug.Log("<color=green>[UltimateUIFixer] Ultimate UI Fix applied successfully.</color>");
        }
    }
}
