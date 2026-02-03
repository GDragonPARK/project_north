using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class KnightFixer : EditorWindow
{
    [MenuItem("Tools/Fix Knight Avatar & Weapon")]
    public static void Execute()
    {
        // 1. Force Humanoid Rig on FBX
        string fbxPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Characters/Knight.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log("Forced Humanoid Rig on Knight.fbx");
            }
        }
        else
        {
            Debug.LogError($"Could not find Knight FBX at {fbxPath}");
            return;
        }

        // 2. Fix Player Animator & Avatar
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            Animator anim = player.GetComponent<Animator>();
            if (anim)
            {
                // Load Avatar from FBX
                Avatar knightAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
                if (knightAvatar) 
                {
                    anim.avatar = knightAvatar;
                    Debug.Log("Assigned Knight Avatar to Animator");
                }
                
                // Ensure Controller
                // Try to find StarterAssets controller
                string[] guids = AssetDatabase.FindAssets("StarterAssetsThirdPerson t:AnimatorController");
                if (guids.Length > 0)
                {
                    string ctrlPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                    if (ctrl) anim.runtimeAnimatorController = ctrl;
                }
            }

            // 3. Fix Ground Layers (Input Check)
            var tpcType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
            if (tpcType != null)
            {
                Component tpc = player.GetComponent(tpcType);
                if (tpc)
                {
                    SerializedObject so = new SerializedObject(tpc);
                    so.Update();
                    // Layer 0 (Default) + Layer 6 (Terrain) usually. 
                    // GroundLayers is LayerMask.
                    // To be safe, set to Everything except Player/IgnoreRaycast? 
                    // Or just Default | Terrain.
                    // LayerMask.GetMask("Default") returns 1. 
                    // Let's set it to Default (1) + Terrain (usually 6 -> 64) if it exists.
                    // Or just -1 (Everything) for now to ensure it works on Altar.
                    
                    int defaultLayer = 1 << LayerMask.NameToLayer("Default");
                    int terrainLayer = 1 << LayerMask.NameToLayer("Terrain");
                    if (terrainLayer == 0) terrainLayer = 0; // If layer doesn't exist

                    // Safety: Add "Default" to whatever is there, or just set strictly.
                    SerializedProperty groundLayers = so.FindProperty("GroundLayers");
                    if (groundLayers != null)
                    {
                        groundLayers.intValue = defaultLayer | terrainLayer; // Force Default + Terrain
                    }
                    so.ApplyModifiedProperties();
                    Debug.Log("Updated Ground Layers to Default + Terrain");
                }
            }

            // 4. Weapon Socket & Sword
            Transform rightHand = FindBone(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindBone(player.transform, "hand.r");
            if (rightHand == null) rightHand = FindBone(player.transform, "Hand_R");

            if (rightHand)
            {
                Transform socket = rightHand.Find("Weapon_Socket");
                if (socket == null)
                {
                    GameObject socketObj = new GameObject("Weapon_Socket");
                    socket = socketObj.transform;
                    socket.SetParent(rightHand);
                    socket.localPosition = new Vector3(0, 0, 0); // Start at hand
                    socket.localRotation = Quaternion.identity;
                }

                // Clean old children
                for (int i = socket.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(socket.GetChild(i).gameObject);
                }

                // Add Sword
                string swordPath = "Assets/valheim_Data/GameElements/Items/weapons/SwordIron.prefab";
                GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(swordPath);
                if (swordPrefab)
                {
                    GameObject swordInstance = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
                    swordInstance.transform.SetParent(socket);
                    swordInstance.transform.localPosition = Vector3.zero;
                    swordInstance.transform.localRotation = Quaternion.Euler(0, 90, 0); // Rough guess for alignment
                    Debug.Log("Equipped Iron Sword");
                }
                else
                {
                    Debug.LogWarning("SwordIron prefab not found.");
                }
            }
            else
            {
                Debug.LogError("Could not find Right Hand bone!");
            }
            
            EditorUtility.SetDirty(player);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Knight Fix Complete!");
    }

    private static Transform FindBone(Transform current, string name)
    {
        if (current.name.Contains(name)) return current;
        foreach (Transform child in current)
        {
            Transform found = FindBone(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
