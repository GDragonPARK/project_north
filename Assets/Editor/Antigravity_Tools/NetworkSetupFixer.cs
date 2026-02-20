using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;
using kcp2k;
using System.Linq;

namespace Antigravity.Tools
{
    public class NetworkSetupFixer : EditorWindow
    {
        private const string MainScenePath = "Assets/1.Scene/Main.unity";
        private const string SampleScenePath = "Assets/1.Scene/SampleScene.unity";
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Fix Network Setup")]
        public static void FixNetworkSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[NetworkSetupFixer] Operation cancelled by user.");
                return;
            }

            // 1. Clean Main Scene
            CleanScene(MainScenePath);
            CleanScene(SampleScenePath);

            // 2. Setup Login Scene
            SetupLoginScene();

            Debug.Log("<color=green>[NetworkSetupFixer] Network Setup completed successfully.</color>");
        }

        private static void CleanScene(string scenePath)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[NetworkSetupFixer] Cannot find scene at {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool modified = false;

            // Find and destroy all NetworkManagers in the active scene
            NetworkManager[] networkManagers = Object.FindObjectsOfType<NetworkManager>(true);
            foreach (var manager in networkManagers)
            {
                if (manager != null && manager.gameObject != null)
                {
                    Debug.Log($"[NetworkSetupFixer] Removing NetworkManager from '{manager.gameObject.name}' in {scene.name}.");
                    Object.DestroyImmediate(manager.gameObject);
                    modified = true;
                }
            }
            
            MySqlAuthenticator[] authenticators = Object.FindObjectsOfType<MySqlAuthenticator>(true);
            foreach (var auth in authenticators)
            {
                if (auth != null && auth.gameObject != null) 
                {
                     Debug.Log($"[NetworkSetupFixer] Removing MySqlAuthenticator from '{auth.gameObject.name}' in {scene.name}.");
                     Object.DestroyImmediate(auth.gameObject);
                     modified = true;
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[NetworkSetupFixer] Cleaned {scene.name} scene.");
            }
        }

        private static void SetupLoginScene()
        {
            if (!System.IO.File.Exists(LoginScenePath))
            {
                Debug.LogError($"[NetworkSetupFixer] Login scene not found at: {LoginScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);

            // 1. Find or create NetworkManager_System
            GameObject nmObj = GameObject.Find("NetworkManager_System");
            if (nmObj == null)
            {
                nmObj = new GameObject("NetworkManager_System");
                Debug.Log("[NetworkSetupFixer] Created new 'NetworkManager_System' object.");
            }

            // 2. Add Required Components
            NetworkManager nm = GetOrAddComponent<NetworkManager>(nmObj);
            KcpTransport transport = GetOrAddComponent<KcpTransport>(nmObj);
            MySqlAuthenticator auth = GetOrAddComponent<MySqlAuthenticator>(nmObj);
            NetworkManagerHUD hud = GetOrAddComponent<NetworkManagerHUD>(nmObj);

            // 3. Link Components using SerializedObject (required for private/internal fields in Mirror)
            SerializedObject serializedNM = new SerializedObject(nm);
            
            SerializedProperty transportProp = serializedNM.FindProperty("transport");
            if (transportProp != null && transportProp.objectReferenceValue != transport)
            {
                transportProp.objectReferenceValue = transport;
                Debug.Log("[NetworkSetupFixer] Linked KcpTransport to NetworkManager.");
            }

            SerializedProperty authProp = serializedNM.FindProperty("authenticator");
            if (authProp != null && authProp.objectReferenceValue != auth)
            {
                authProp.objectReferenceValue = auth;
                Debug.Log("[NetworkSetupFixer] Linked MySqlAuthenticator to NetworkManager.");
            }

            // Make sure player prefab field is cleared if it points to a missing or wrong object
            // Setting this explicitly requires matching the project's exact class and structure, 
            // but for now, we just ensure transport and auth are linked.

            // Enable "Don't Destroy On Load" option in NetworkManager
            SerializedProperty dontDestroyProp = serializedNM.FindProperty("dontDestroyOnLoad");
            if (dontDestroyProp != null && dontDestroyProp.boolValue == false)
            {
                dontDestroyProp.boolValue = true;
            }

            serializedNM.ApplyModifiedProperties();

            // Set scene dirty
            EditorUtility.SetDirty(nm);
            EditorUtility.SetDirty(nmObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            Debug.Log("[NetworkSetupFixer] Setup LoginScene completed.");
            
            Selection.activeGameObject = nmObj;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}
