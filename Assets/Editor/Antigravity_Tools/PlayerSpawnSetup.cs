using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;

namespace Antigravity.Tools
{
    public class PlayerSpawnSetup : EditorWindow
    {
        private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

        [MenuItem("Tools/Project North/Setup Player Spawn")]
        public static void SetupPlayerSpawn()
        {
            // 1. Prefab Setup (NetworkIdentity 부착)
            string[] guids = AssetDatabase.FindAssets("Player_New t:Prefab");
            if (guids.Length == 0)
            {
                Debug.LogError("[PlayerSpawnSetup] Could not find prefab named 'Player_New'.");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            Debug.Log($"[PlayerSpawnSetup] Found Player_New prefab at: {prefabPath}");

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError("[PlayerSpawnSetup] Failed to load prefab contents.");
                return;
            }

            bool needsSave = false;
            NetworkIdentity identity = prefabRoot.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                prefabRoot.AddComponent<NetworkIdentity>();
                needsSave = true;
                Debug.Log("[PlayerSpawnSetup] Added NetworkIdentity to Player_New prefab.");
            }

            if (needsSave)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log("[PlayerSpawnSetup] Saved Player_New prefab.");
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            // Need to load the actual prefab asset to assign to NetworkManager
            GameObject actualPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // 2. NetworkManager Link (프리팹 할당)
            if (EditorSceneManager.GetActiveScene().path != LoginScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[PlayerSpawnSetup] Operation cancelled by user.");
                    return;
                }
                EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
            }

            GameObject nmObj = GameObject.Find("NetworkManager_System");
            if (nmObj == null)
            {
                Debug.LogError("[PlayerSpawnSetup] 'NetworkManager_System' object not found in LoginScene.");
                return;
            }

            NetworkManager nm = nmObj.GetComponent<NetworkManager>();
            if (nm == null)
            {
                Debug.LogError("[PlayerSpawnSetup] 'NetworkManager' component not found on 'NetworkManager_System'.");
                return;
            }

            SerializedObject serializedNM = new SerializedObject(nm);
            SerializedProperty playerPrefabProp = serializedNM.FindProperty("playerPrefab");

            if (playerPrefabProp != null)
            {
                playerPrefabProp.objectReferenceValue = actualPrefabAsset;
                serializedNM.ApplyModifiedProperties();
                Debug.Log("[PlayerSpawnSetup] Successfully assigned Player_New to NetworkManager.playerPrefab.");
                
                EditorUtility.SetDirty(nm);
                EditorUtility.SetDirty(nmObj);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            else
            {
                Debug.LogError("[PlayerSpawnSetup] Field 'playerPrefab' not found in NetworkManager.");
            }

            Debug.Log("<color=green>[PlayerSpawnSetup] Setup Player Spawn completed successfully.</color>");
            Selection.activeGameObject = nmObj;
        }
    }
}
