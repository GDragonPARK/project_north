using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class PhysicalButtonFixer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Force Physical Rebuild")]
        public static void ForcePhysicalRebuild()
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
                    Debug.LogError($"[PhysicalButtonFixer] Login scene not found at: {LoginScenePath}");
                    return;
                }
            }

            var scene = EditorSceneManager.GetActiveScene();

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

            if (connectBtn == null)
            {
                GameObject btnObj = GameObject.Find("ConnectButton");
                if (btnObj != null) connectBtn = btnObj.GetComponent<Button>();
            }

            if (connectBtn == null)
            {
                Debug.LogError("[PhysicalButtonFixer] ConnectButton not found!");
                return;
            }

            // Report current state
            RectTransform rt = connectBtn.GetComponent<RectTransform>();
            Debug.Log($"[PhysicalButtonFixer] BEFORE - sizeDelta: {rt.sizeDelta}, rect: {rt.rect.width}x{rt.rect.height}");

            // A) LayoutElement - force minimum size, block shrinking
            LayoutElement layoutElement = connectBtn.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = connectBtn.gameObject.AddComponent<LayoutElement>();
                Debug.Log("[PhysicalButtonFixer] Added LayoutElement.");
            }
            layoutElement.minWidth = 200f;
            layoutElement.minHeight = 60f;
            layoutElement.preferredWidth = 200f;
            layoutElement.preferredHeight = 60f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
            EditorUtility.SetDirty(layoutElement);

            // B) Force RectTransform sizeDelta
            rt.sizeDelta = new Vector2(200f, 60f);
            EditorUtility.SetDirty(rt);
            Debug.Log($"[PhysicalButtonFixer] AFTER - sizeDelta forced to: {rt.sizeDelta}");

            // C) Make Image fully opaque RED for visibility
            Image btnImage = connectBtn.GetComponent<Image>();
            if (btnImage == null)
            {
                btnImage = connectBtn.gameObject.AddComponent<Image>();
                Debug.Log("[PhysicalButtonFixer] Added missing Image component.");
            }
            btnImage.color = Color.red; // Fully opaque red
            EditorUtility.SetDirty(btnImage);
            Debug.Log("[PhysicalButtonFixer] Set button Image to opaque RED for visibility.");

            // D) Guarantee raycastTarget
            btnImage.raycastTarget = true;
            EditorUtility.SetDirty(btnImage);

            // E) Ensure UIHoverFeedback is attached
            if (connectBtn.GetComponent<UIHoverFeedback>() == null)
            {
                connectBtn.gameObject.AddComponent<UIHoverFeedback>();
                Debug.Log("[PhysicalButtonFixer] Attached UIHoverFeedback component.");
                EditorUtility.SetDirty(connectBtn.gameObject);
            }

            // Also ensure CanvasGroup bypass is still intact
            CanvasGroup cg = connectBtn.GetComponent<CanvasGroup>();
            if (cg == null) cg = connectBtn.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.ignoreParentGroups = true;
            EditorUtility.SetDirty(cg);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = connectBtn.gameObject;
            Debug.Log("<color=green>[PhysicalButtonFixer] Connect 버튼 물리 크기 강제 복구 완료!</color>");
        }
    }
}
