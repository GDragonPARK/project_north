using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace Antigravity.Tools
{
    public class LoginUIFixer : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Fix Login UI")]
        public static void FixLoginUI()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[LoginUIFixer] Operation cancelled by user.");
                return;
            }

            if (!System.IO.File.Exists(LoginScenePath))
            {
                Debug.LogError($"[LoginUIFixer] Login scene not found at: {LoginScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);

            // 1. Find the Connect Panel by searching roughly for the existing InputArea
            // We know from user context, ConnectPanel contains InputArea. We can find the script first.
            LoginUIController controller = Object.FindAnyObjectByType<LoginUIController>();
            if (controller == null)
            {
                Debug.LogError("[LoginUIFixer] LoginUIController not found in the scene.");
                return;
            }

            // Let's find "InputArea" to duplicate it. Or we just create a new GameObject inside "ConnectPanel".
            GameObject connectPanel = null;
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            Transform existingInputArea = null;
            foreach (var t in transforms)
            {
                if (t.name == "InputArea")
                {
                    existingInputArea = t;
                    connectPanel = t.parent.gameObject;
                    break;
                }
            }

            if (existingInputArea == null)
            {
                // Unlikely but if missing, just use Canvas directly or fail
                Debug.LogError("[LoginUIFixer] 'InputArea' not found. Cannot determine where to insert AuthInputArea.");
                return;
            }

            // Also check if AuthInputArea already exists
            Transform existingAuthArea = connectPanel.transform.Find("AuthInputArea");
            if (existingAuthArea != null)
            {
                 Debug.LogWarning("[LoginUIFixer] 'AuthInputArea' already exists. Removing it to regenerate.");
                 Object.DestroyImmediate(existingAuthArea.gameObject);
            }

            // 2. Duplicate InputArea to preserve exact layout and styling
            GameObject authInputAreaObj = GameObject.Instantiate(existingInputArea.gameObject, connectPanel.transform);
            authInputAreaObj.name = "AuthInputArea";

            // Position it right below InputArea (so index = existingInputArea.GetSiblingIndex() + 1)
            authInputAreaObj.transform.SetSiblingIndex(existingInputArea.GetSiblingIndex() + 1);

            // 3. Rename inputs and modify placeholders and content types
            TMP_InputField[] inputFields = authInputAreaObj.GetComponentsInChildren<TMP_InputField>(true);
            if (inputFields.Length >= 2)
            {
                TMP_InputField idInput = inputFields[0];
                TMP_InputField pwInput = inputFields[1];

                idInput.gameObject.name = "ID_InputField";
                pwInput.gameObject.name = "PW_InputField";

                // Set placeholders (assuming standard TextMeshPro structure)
                var idPlaceholder = idInput.placeholder as TextMeshProUGUI;
                if (idPlaceholder != null) idPlaceholder.text = "ID";
                idInput.text = ""; // clear text
                
                var pwPlaceholder = pwInput.placeholder as TextMeshProUGUI;
                if (pwPlaceholder != null) pwPlaceholder.text = "Password";
                pwInput.text = ""; // clear text
                pwInput.contentType = TMP_InputField.ContentType.Password;

                // 4. Bind to LoginUIController
                SerializedObject serializedController = new SerializedObject(controller);
                
                SerializedProperty idProp = serializedController.FindProperty("idInputField");
                if (idProp != null)
                {
                    idProp.objectReferenceValue = idInput;
                }

                SerializedProperty pwProp = serializedController.FindProperty("pwInputField");
                if (pwProp != null)
                {
                    pwProp.objectReferenceValue = pwInput;
                }

                serializedController.ApplyModifiedProperties();
                Debug.Log("[LoginUIFixer] Successfully bound ID and PW InputFields to the LoginUIController.");
            }
            else
            {
                Debug.LogError("[LoginUIFixer] Duplicated area did not have 2 InputFields to bind as ID/PW.");
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(connectPanel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=green>[LoginUIFixer] Login UI setup completed successfully.</color>");
            Selection.activeGameObject = authInputAreaObj;
        }
    }
}
