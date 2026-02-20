using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Antigravity.Tools
{
    public class HitboxVisualizer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Fix & Visualize Hitbox")]
        public static void FixAndVisualize()
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
                    Debug.LogError($"[HitboxVisualizer] Login scene not found at: {LoginScenePath}");
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
                Debug.LogError("[HitboxVisualizer] ConnectButton could not be found!");
                return;
            }

            // A & B) Find Image and set Color to semi-transparent green
            Image btnImage = connectBtn.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = new Color(0f, 1f, 0f, 0.3f);
                EditorUtility.SetDirty(btnImage);
                Debug.Log("[HitboxVisualizer] Changed ConnectButton color to semi-transparent green.");
                isModified = true;
            }

            // C) Add/Configure LayoutElement
            LayoutElement layoutElement = connectBtn.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = connectBtn.gameObject.AddComponent<LayoutElement>();
                Debug.Log("[HitboxVisualizer] Added LayoutElement to ConnectButton.");
            }

            layoutElement.minWidth = 200f;
            layoutElement.minHeight = 60f;
            layoutElement.preferredWidth = 200f;
            layoutElement.preferredHeight = 60f;
            EditorUtility.SetDirty(layoutElement);
            Debug.Log("[HitboxVisualizer] Configured LayoutElement dimensions (200x60).");
            isModified = true;

            // D) Restore child Text raycastTarget = true
            Graphic[] childGraphics = connectBtn.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in childGraphics)
            {
                if (g.gameObject != connectBtn.gameObject) // Skip the button itself
                {
                    if (!g.raycastTarget)
                    {
                        g.raycastTarget = true;
                        EditorUtility.SetDirty(g);
                        Debug.Log($"[HitboxVisualizer] Enabled raycastTarget on child Graphic: {g.gameObject.name}");
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
            Debug.Log("<color=green>[HitboxVisualizer] Connect 버튼 히트박스 가시화 및 크기 확보 완료!</color>");
        }
    }
}
