using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEditor.SceneManagement;

public class ProjectAutoSetup : EditorWindow
{
    [MenuItem("Tools/Auto Setup/Execute All (Instant)")]
    public static void ExecuteAll()
    {
        Debug.Log("Starting One-Shot Auto Setup...");
        
        // 1. Core Setup
        // 1. Core Setup
        SetupEnvironmentInScene();
        // SetupPlayerPrefab(); // Disabled to preserve manual "Brain" setup
 
        // 2. Camera System
        // Cinemachine.Editor.CameraSystemUpgrader.UpgradeCamera(); // Disabled to preserve manual target assignments

        // 3. UI System
        Antigravity.Editor.UISystemPolisher.PolishUI();
        
        // 4. Animation Linking
        Antigravity.Editor.AnimationAutoLinker.LinkAnimations();

        // 5. Atmosphere
        // Assuming we have a tool for this as mentioned in walkthrough
        // If not, we skip or call if we can find the class.
        // Based on history, "Tools/Setup Atmosphere" was mentioned.
        // Let's try to find it dynamically or just skip if not easily accessible static method.
        // Actually, let's look for "AtmosphereSetup" type.
        var atmosType = System.Type.GetType("AtmosphereSetup");
        if (atmosType != null)
        {
             var method = atmosType.GetMethod("SetupGlobalVolume", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
             if (method != null) method.Invoke(null, null);
        }

        // 6. Spawn Vegetation (Instance)
        VegetationSpawner spawner = Object.FindFirstObjectByType<VegetationSpawner>();
        if (spawner)
        {
            spawner.SpawnGrass();
        }
        else
        {
            Debug.LogWarning("VegetationSpawner not found in scene!");
        }

        // 7. Save Scene
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Scene Saved. Auto-Setup Complete! Press Play.");
    }

    [MenuItem("Tools/Setup Player Prefab")]
    public static void SetupPlayerPrefab()
    {
        // 1. Check if Player exists in scene first to avoid duplicates
        GameObject instance = GameObject.Find("Player_New");
        if (instance == null) instance = GameObject.FindGameObjectWithTag("Player");
        
        if (instance == null)
        {
            string prefabPath = "Assets/2.Model/Prefabs/Characters/Player/Player.prefab";
            GameObject prefabBase = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefabBase == null)
            {
                 prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
                 prefabBase = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            
            if (prefabBase != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabBase);
                instance.name = "Player_New";
            }
            else
            {
                Debug.LogError("Player Prefab base not found! Creating empty.");
                instance = new GameObject("Player_New");
            }
        }
        else
        {
            instance.name = "Player_New"; // Enforce name
        }

        instance.tag = "Player";
        
        // --- COMPONENT INJECTION ---

        // 1. CharacterController (Preserve & Configure)
        CharacterController cc = instance.GetComponent<CharacterController>();
        if (cc == null) cc = instance.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1.0f, 0);
        cc.height = 2.0f;
        cc.radius = 0.5f;

        // 2. Add and setup Rigidbody (Kinematic for TPC usually, or Gravity aware)
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null) rb = instance.AddComponent<Rigidbody>();
        rb.useGravity = true; 
        
        // 3. Add and setup PlayerInput
        PlayerInput pi = instance.GetComponent<PlayerInput>();
        if (pi == null) pi = instance.AddComponent<PlayerInput>();
        
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem/StarterAssets.inputactions"); 
        if (actions == null) actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/StarterAssets/InputSystem/StarterAssets.inputactions");
        
        if (actions != null) 
        {
            pi.actions = actions;
            pi.defaultControlScheme = "KeyboardMouse";
        }

        // 4. Starter Assets Inputs
        System.Type inputsType = System.Type.GetType("StarterAssets.StarterAssetsInputs, Assembly-CSharp"); 
        if (inputsType == null) inputsType = System.Type.GetType("StarterAssetsInputs, Assembly-CSharp");
        
        if (inputsType != null) 
        {
            if (instance.GetComponent(inputsType) == null) instance.AddComponent(inputsType);
        }
        else Debug.LogError("StarterAssetsInputs Type not found");

        // 5. ThirdPersonController (The Brain)
        System.Type tpcType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
        if (tpcType == null) tpcType = System.Type.GetType("ThirdPersonController, Assembly-CSharp");
             
        if (tpcType != null) 
        {
             Component tpc = instance.GetComponent(tpcType);
             if (tpc == null) tpc = instance.AddComponent(tpcType);
             
             SerializedObject so = new SerializedObject(tpc);
             so.Update();
             so.FindProperty("GroundLayers").intValue = LayerMask.GetMask("Default", "Terrain");
             so.FindProperty("GroundedOffset").floatValue = -0.14f;
             so.FindProperty("GroundedRadius").floatValue = 0.28f;
             
             // Assign CinemachineCameraTarget
             Transform cameraRoot = instance.transform.Find("PlayerCameraRoot");
             if (cameraRoot == null)
             {
                 GameObject newRoot = new GameObject("PlayerCameraRoot");
                 newRoot.transform.SetParent(instance.transform);
                 newRoot.transform.localPosition = new Vector3(0, 1.375f, 0); // Standard offset
                 cameraRoot = newRoot.transform;
             }
             so.FindProperty("CinemachineCameraTarget").objectReferenceValue = cameraRoot.gameObject;

             so.ApplyModifiedProperties();
        }
        else Debug.LogError("ThirdPersonController Type not found");

        // 6. Animator
        Animator anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        
        // Controller - Force StarterAssets one
        if (anim.runtimeAnimatorController == null || !anim.runtimeAnimatorController.name.Contains("Starter"))
        {
            // Try specific path first
            string path = "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            
            if (rac == null)
            {
                // Search fallback
                string[] guids = AssetDatabase.FindAssets("StarterAssetsThirdPerson t:AnimatorController");
                if (guids.Length > 0)
                    rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            
            if (rac != null) anim.runtimeAnimatorController = rac;
            else Debug.LogError("StarterAssetsThirdPerson Controller NOT FOUND. Please import StarterAssets.");
        }

        // Avatar
        if (anim.avatar == null)
        {
             string[] avatars = AssetDatabase.FindAssets("Knight t:Avatar");
             if (avatars.Length > 0) anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AssetDatabase.GUIDToAssetPath(avatars[0]));
        }

        // 7. MyPlayerController
        if (instance.GetComponent<MyPlayerController>() == null)
            instance.AddComponent<MyPlayerController>();

        Debug.Log("Player_New Setup Complete: Brains Injected.");
        
        // Dirty Check
        EditorUtility.SetDirty(instance);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Setup Environment in Scene")]
    public static void SetupEnvironmentInScene()
    {
        GameObject env = GameObject.Find("Environment");
        if (env == null)
            env = new GameObject("Environment");

        // Add Terrain Generator
        if (env.GetComponent<TerrainGenerator>() == null)
            env.AddComponent<TerrainGenerator>();

        // Add Vegetation Spawner
        VegetationSpawner spawner = env.GetComponent<VegetationSpawner>();
        if (spawner == null)
            spawner = env.AddComponent<VegetationSpawner>();

        // Assign some default tree prefabs if possible
        string[] treeGuids = AssetDatabase.FindAssets("t:Prefab FirTree");
        if (treeGuids.Length > 0)
        {
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(treeGuids[0]));
            spawner.treePrefabs = new System.Collections.Generic.List<GameObject> { treePrefab };
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Environment setup in active scene.");
    }
}
