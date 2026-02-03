using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class KnightIdentityRestorer : EditorWindow
{
    [MenuItem("Tools/Restorer/Fix Knight Rig & Physics")]
    public static void ExecuteFix()
    {
        // 1. Force 'CreateFromThisModel' on Knight FBX
        string fbxPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Characters/Knight.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            // Always reset to Human and CreateFromThisModel to purge "CopyFromOther"
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log($"<color=green>FIXED: Generated unique Avatar for {fbxPath} (CreateFromThisModel)</color>");
        }
        else
        {
            Debug.LogError($"Cannot find FBX at {fbxPath}");
            return;
        }

        // 2. Assign New Avatar & Cleanup Physics
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            // A. Update Animator
            Animator anim = player.GetComponent<Animator>();
            if (anim)
            {
                // Reload avatar from the re-imported asset
                Avatar newAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
                if (newAvatar)
                {
                    anim.avatar = newAvatar;
                    Debug.Log("Assigned newly generated Avatar to Player Animator.");
                }
                
                // Re-verify Controller
                if (anim.runtimeAnimatorController == null)
                {
                   string[] guids = AssetDatabase.FindAssets("StarterAssetsThirdPerson t:AnimatorController");
                   if (guids.Length > 0)
                       anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            // B. Remove Rigidbody (Conflict with CharacterController)
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
                Debug.Log("Removed conflicting Rigidbody component.");
            }

            // C. Fix Ground Layers
            var tpcType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
            if (tpcType != null)
            {
                Component tpc = player.GetComponent(tpcType);
                if (tpc)
                {
                    SerializedObject so = new SerializedObject(tpc);
                    so.Update();
                    SerializedProperty gl = so.FindProperty("GroundLayers");
                    // Set to Everything (-1)
                    if (gl != null) gl.intValue = -1;
                    so.ApplyModifiedProperties();
                    Debug.Log("Set Ground Layers to Everything (-1).");
                }
            }

            // D. Equip Weapon (Axe or Sword)
            EquipWeapon(player.transform);
            
            EditorUtility.SetDirty(player);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Knight Identity Restoration Complete!");
    }

    private static void EquipWeapon(Transform root)
    {
        Transform rightHand = FindBone(root, "RightHand");
        if (rightHand == null) rightHand = FindBone(root, "hand.r");
        
        if (rightHand)
        {
            Transform socket = rightHand.Find("Weapon_Socket");
            if (socket == null)
            {
                GameObject obj = new GameObject("Weapon_Socket");
                socket = obj.transform;
                socket.SetParent(rightHand);
                socket.localPosition = Vector3.zero;
                socket.localRotation = Quaternion.identity;
            }

            // Clear old
            for (int i = socket.childCount - 1; i >= 0; i--) DestroyImmediate(socket.GetChild(i).gameObject);

            // Find Axe first, then Sword
            GameObject weaponPrefab = FindPrefab("Axe");
            if (weaponPrefab == null) weaponPrefab = FindPrefab("Sword");
            
            if (weaponPrefab)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab);
                instance.transform.SetParent(socket);
                instance.transform.localPosition = Vector3.zero;
                // Adjust Rotation based on common prefabs (often Y-up or Z-forward)
                // KayKit weapons usually need specific rotation. 
                // Let's try identity first, usually user can tweak. 
                // But generally swords in hand need (0, 90, 0) or (-90, 0, 0).
                // Let's stick to (0,0,0) as base unless we know specific prefab orientation.
                // Assuming standard "point up" alignment.
                instance.transform.localRotation = Quaternion.Euler(0, 90, 0); 
                Debug.Log($"Equipped {weaponPrefab.name}");
            }
        }
    }

    private static GameObject FindPrefab(string nameContains)
    {
        string[] guids = AssetDatabase.FindAssets($"{nameContains} t:Prefab");
        foreach(var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains("KayKit") || p.Contains("GameElements")) // Prefer game assets
            {
                 // Filter out "Icon" or "Visual" if strictly needed, but nameContains usually works.
                 // Prefer "AxeWood" or "AxeStone" over "Axe_Icon"
                 if(p.ToLower().Contains("icon")) continue;
                 return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
        }
        return null;
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
