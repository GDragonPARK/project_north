using UnityEngine;
using UnityEditor;

public class PlayerSystemUpgrader : EditorWindow
{
    [MenuItem("Tools/Upgrade Player System")]
    public static void UpgradePlayer()
    {
        // 1. Load Player Prefab
        string playerPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject validation = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        
        // Fallback or User-moved path
        if (validation == null)
        {
             string[] guids = AssetDatabase.FindAssets("PlayerArmature t:Prefab");
             if (guids.Length > 0) playerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
             else 
             {
                 playerPath = "Assets/2.Model/Prefabs/Characters/Player/Player.prefab";
                 if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) == null)
                 {
                    Debug.LogError("PlayerArmature prefab not found!");
                    return;
                 }
             }
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(playerPath);

        // 2. Load Knight Model
        string[] knightGUIDs = AssetDatabase.FindAssets("Knight t:Model");
        if (knightGUIDs.Length == 0)
        {
            Debug.LogError("Knight model not found!");
            return;
        }
        string knightGUID = knightGUIDs[0];
        string knightPath = AssetDatabase.GUIDToAssetPath(knightGUID);

        // Ensure Rig is Humanoid
        ModelImporter importer = AssetImporter.GetAtPath(knightPath) as ModelImporter;
        if (importer && importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log("Forced Knight rig to Humanoid.");
        }

        GameObject knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(knightPath);

        // 3. Remove Old Geometry
        string[] oldNames = new string[] { "Geometry", "Skeleton", "BoxMan", "Armature_Old", "Knight_Visuals" };
        foreach(var name in oldNames)
        {
            Transform t = contents.transform.Find(name);
            if (t) DestroyImmediate(t.gameObject);
        }

        // 4. Instantiate Knight
        GameObject knight = (GameObject)PrefabUtility.InstantiatePrefab(knightPrefab);
        knight.transform.SetParent(contents.transform);
        knight.transform.localPosition = Vector3.zero;
        knight.transform.localRotation = Quaternion.identity;
        knight.name = "Knight_Visuals";

        // 5. Update Animator
        Animator anim = contents.GetComponent<Animator>();
        if (anim)
        {
            // Try to load Avatar directly from the model asset
            Avatar targetAvatar = null;
            
            // 1. Try Component on Instantiated Prefab (Best for "Copy From Other Avatar")
            Animator knightAnim = knight.GetComponent<Animator>();
            if (knightAnim && knightAnim.avatar)
            {
                targetAvatar = knightAnim.avatar;
            }
            
            // 2. If null, try sub-assets
            if (targetAvatar == null)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(knightGUID));
                foreach (Object o in assets)
                {
                    if (o is Avatar)
                    {
                        targetAvatar = o as Avatar;
                        break;
                    }
                }
            }

            if (targetAvatar != null)
            {
                anim.avatar = targetAvatar;
                anim.applyRootMotion = true; 
                Debug.Log($"Updated Animator Avatar to Knight using: {targetAvatar.name}");
            }
            else
            {
                 Debug.LogWarning("Could not find Avatar for Knight! ensure rig is set to Humanoid. Proceeding with rest of setup...");
            }
        }

        // 6. Fix Camera Occlusion
        Transform camRoot = contents.transform.Find("PlayerCameraRoot");
        if (camRoot)
        {
            camRoot.localPosition = new Vector3(0, 1.6f, 0); 
        }

        // 7. Create Weapon Socket
        Transform handR = FindDeepChild(knight.transform, "hand.R");
        if (handR == null) handR = FindDeepChild(knight.transform, "RightHand");
        if (handR == null) handR = FindDeepChild(knight.transform, "Hand_R");

        if (handR)
        {
             Transform existingSocket = handR.Find("Weapon_Socket");
             if (existingSocket) DestroyImmediate(existingSocket.gameObject);

             GameObject socket = new GameObject("Weapon_Socket");
             socket.transform.SetParent(handR);
             socket.transform.localPosition = Vector3.zero;
             socket.transform.localRotation = Quaternion.identity;

             // 8. Equip Sword (Default)
             string[] swordGUIDs = AssetDatabase.FindAssets("sword_1handed t:Model"); 
             if (swordGUIDs.Length > 0)
             {
                 GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(swordGUIDs[0])));
                 sword.transform.SetParent(socket.transform);
                 sword.transform.localPosition = new Vector3(0, 0, 0); 
                 sword.transform.localRotation = Quaternion.Euler(0, 90, 0); 
             }
             else
             {
                 Debug.LogWarning("Sword model not found. Skipping default equip.");
             }
        }
        else
        {
            Debug.LogError("Could not find Right Hand bone!");
        }

        // Save
        PrefabUtility.SaveAsPrefabAsset(contents, playerPath);
        PrefabUtility.UnloadPrefabContents(contents);
        
        // 10. Update Scene Instance
        GameObject scenePlayer = GameObject.Find("Player") ?? GameObject.Find("PlayerArmature");
        if (scenePlayer)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (PrefabUtility.GetCorrespondingObjectFromSource(scenePlayer) == prefabAsset)
            {
                PrefabUtility.RevertObjectOverride(scenePlayer, InteractionMode.AutomatedAction);
                Debug.Log("Reverted Scene Player to match updated Prefab.");
            }
        }
        
        Debug.Log("Player Upgraded to Knight successfully!");
    }

    private static Transform FindDeepChild(Transform aParent, string aName)
    {
        foreach(Transform child in aParent)
        {
            if(child.name.IndexOf(aName, System.StringComparison.OrdinalIgnoreCase) >= 0) return child;
            var result = FindDeepChild(child, aName);
            if (result != null) return result;
        }
        return null;
    }
}