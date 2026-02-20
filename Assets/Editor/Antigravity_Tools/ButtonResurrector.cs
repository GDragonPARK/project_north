using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class ButtonResurrector : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Resurrect Connect Button")]
        public static void ResurrectButton()
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
                    Debug.LogError($"[ButtonResurrector] Login scene not found at: {LoginScenePath}");
                    return;
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            bool isModified = false;

            // Find the Connect Button
            Button connectBtn = null;
            LoginUIController controller = Object.FindAnyObjectByType<LoginUIController>();
            
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty connectBtnProp = so.FindProperty("connectButton");
                if (connectBtnProp != null && connectBtnProp.objectReferenceValue != null)
                {
                    connectBtn = connectBtnProp.objectReferenceValue as Button;
                }
            }

            // Fallback finding by name
            if (connectBtn == null)
            {
                GameObject btnObj = GameObject.Find("ConnectButton");
                if (btnObj != null)
                {
                    connectBtn = btnObj.GetComponent<Button>();
                }
            }

            if (connectBtn == null)
            {
                Debug.LogError("[ButtonResurrector] ConnectButton could not be found!");
                return;
            }

            // A) Force raycastTarget = true on button Image
            Image btnImage = connectBtn.GetComponent<Image>();
            if (btnImage != null)
            {
                if (!btnImage.raycastTarget)
                {
                    btnImage.raycastTarget = true;
                    EditorUtility.SetDirty(btnImage);
                    Debug.Log("[ButtonResurrector] Forced raycastTarget = true on ConnectButton Image.");
                    isModified = true;
                }
            }
            else
            {
                Debug.LogWarning("[ButtonResurrector] ConnectButton has no Image component!");
            }

            // B) Force raycastTarget = false on child Text/TextMeshPro
            Graphic[] childGraphics = connectBtn.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in childGraphics)
            {
                if (g.gameObject != connectBtn.gameObject) // Skip the button itself
                {
                    if (g.raycastTarget)
                    {
                        g.raycastTarget = false;
                        EditorUtility.SetDirty(g);
                        Debug.Log($"[ButtonResurrector] Disabled raycastTarget on child Graphic: {g.gameObject.name}");
                        isModified = true;
                    }
                }
            }

            // C) [Most Important] Add CanvasGroup and set ignoreParentGroups = true
            CanvasGroup canvasGroup = connectBtn.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = connectBtn.gameObject.AddComponent<CanvasGroup>();
                Debug.Log("[ButtonResurrector] Added CanvasGroup to ConnectButton.");
                isModified = true;
            }

            if (!canvasGroup.interactable || !canvasGroup.blocksRaycasts || !canvasGroup.ignoreParentGroups)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.ignoreParentGroups = true; // FORCE bypass of any parent panel restrictions
                EditorUtility.SetDirty(canvasGroup);
                Debug.Log("[ButtonResurrector] Configured CanvasGroup: interactable=true, blocksRaycasts=true, ignoreParentGroups=true.");
                isModified = true;
            }

            // D) Disable raycastTarget on potentially overlapping background/status elements
            string[] overlappingNames = { "StatusText", "LogArea", "Footer", "ConnectPanel", "TitlePanel" };
            foreach (string objName in overlappingNames)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null)
                {
                    Graphic graphic = obj.GetComponent<Graphic>();
                    if (graphic != null && graphic.raycastTarget)
                    {
                        graphic.raycastTarget = false;
                        EditorUtility.SetDirty(graphic);
                        Debug.Log($"[ButtonResurrector] Disabled raycastTarget on potential overlap: {objName}");
                        isModified = true;
                    }

                    // Also check for image components on panels that might not be the root graphic
                    Image img = obj.GetComponent<Image>();
                    if (img != null && img.raycastTarget)
                    {
                        img.raycastTarget = false;
                        EditorUtility.SetDirty(img);
                        Debug.Log($"[ButtonResurrector] Disabled raycastTarget on Image overlap: {objName}");
                        isModified = true;
                    }
                }
            }

            if (isModified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Selection.activeGameObject = connectBtn.gameObject;
            Debug.Log("<color=green>[ButtonResurrector] Connect 버튼 완전 복구 완료!</color>");
        }
    }
}
