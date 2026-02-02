using UnityEngine;
using UnityEditor;
using StarterAssets;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

public class PlayerBuilder : EditorWindow
{
    [MenuItem("Tools/Build New Player")]
    public static void Build()
    {
        // 1. Disable Old Player
        GameObject oldPlayer = GameObject.Find("Player") ?? GameObject.Find("PlayerArmature");
        if (oldPlayer) 
        {
            oldPlayer.SetActive(false);
            Debug.Log("Disabled old Player object.");
        }

        // 2. Create New Player Root
        GameObject p = new GameObject("Player_New");
        p.tag = "Player";
        
        // 3. Components
        CharacterController cc = p.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0.9f, 0);
        cc.height = 1.8f;
        cc.radius = 0.28f;
        cc.minMoveDistance = 0f;

        Animator anim = p.AddComponent<Animator>();
        
        // Add StarterAssets Inputs
        p.AddComponent<PlayerInput>(); 
        // Note: ThirdPersonController requires Setup but we'll add it.
        // We might need to copy logic/settings from old player or use defaults.
        // Assuming standard StarterAssets config:
        if (oldPlayer)
        {
            // Copy PlayerInput settings if possible (Actions asset)
            PlayerInput oldInput = oldPlayer.GetComponent<PlayerInput>();
            if (oldInput) p.GetComponent<PlayerInput>().actions = oldInput.actions;
        }

        GameObject cameraRoot = new GameObject("PlayerCameraRoot");
        cameraRoot.transform.SetParent(p.transform);
        cameraRoot.transform.localPosition = new Vector3(0, 1.375f, 0); // Standard eye height

        // 4. Knight Visuals
        string[] knightGUIDs = AssetDatabase.FindAssets("Knight t:Model");
        if (knightGUIDs.Length > 0)
        {
            GameObject knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(knightGUIDs[0]));
            GameObject knightVO = (GameObject)PrefabUtility.InstantiatePrefab(knightPrefab);
            knightVO.name = "Knight_Visuals";
            knightVO.transform.SetParent(p.transform);
            knightVO.transform.localPosition = Vector3.zero;
            knightVO.transform.localRotation = Quaternion.identity;

            // Setup Avatar
            Animator knightAnim = knightVO.GetComponent<Animator>();
            if (knightAnim && knightAnim.avatar)
            {
                anim.avatar = knightAnim.avatar;
                Debug.Log("Assigned Knight Avatar.");
            }
        }

        // 5. Controller Script
        // Use Type.GetType to avoid assembly compilation issues in Editor script string
        // But for direct script we can try AddComponent string or strict type if assembly ref works.
        // Assuming "StarterAssets" assembly:
        var tpc = p.AddComponent<ThirdPersonController>();
        tpc.CinemachineCameraTarget = cameraRoot;
        tpc.Gravity = -15.0f;
        tpc.JumpHeight = 1.2f;
        tpc.MoveSpeed = 2.0f;
        tpc.SprintSpeed = 5.335f;
        tpc.RotationSmoothTime = 0.12f;
        tpc.SpeedChangeRate = 10.0f;

        // Assign Animator Controller
        // Looking for "ThirdPersonAnimatorController"
        string[] animConGUIDs = AssetDatabase.FindAssets("ThirdPersonAnimatorController t:AnimatorController");
        if (animConGUIDs.Length > 0)
        {
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(animConGUIDs[0]));
        }

        // 6. Camera Follow
        GameObject mainCam = GameObject.Find("Main Camera"); // Or Cinemachine brain?
        // Finding Cinemachine Virtual Camera
        // Since we don't have direct Cinemachine types in this script (maybe), 
        // we can look for object with "PlayerFollowCamera" name usually in StarterAssets
        GameObject vCam = GameObject.Find("PlayerFollowCamera");
        if (vCam)
        {
            // Reflection or SerializedObject to set Follow/LookAt if types missing
             SerializedObject so = new SerializedObject(vCam.GetComponent("CinemachineVirtualCamera"));
             so.FindProperty("m_Follow").objectReferenceValue = cameraRoot.transform;
             so.FindProperty("m_LookAt").objectReferenceValue = cameraRoot.transform;
             so.ApplyModifiedProperties();
             Debug.Log("Updated Cinemachine Target.");
        }

        // 7. Weapon Socket & Manager
        if (p.transform.Find("Knight_Visuals"))
        {
             Transform handR = FindDeepChild(p.transform, "hand.R");
             if (handR == null) handR = FindDeepChild(p.transform, "RightHand");
             
             if (handR)
             {
                 GameObject socket = new GameObject("Weapon_Socket");
                 socket.transform.SetParent(handR);
                 socket.transform.localPosition = Vector3.zero;
                 socket.transform.localRotation = Quaternion.identity;
                 
                 // Add Equipment Manager
                 Equipment_Manager em = p.AddComponent<Equipment_Manager>();
                 em.weaponSocketRight = socket.transform;
                 
                 Debug.Log("Created Weapon Socket and attached Equipment_Manager.");
             }
        }

        Selection.activeGameObject = p;
        Debug.Log("New Player Created!");
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