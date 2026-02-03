using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ForceFixPlayer : EditorWindow
{
    [MenuItem("Tools/Force Fix Player (NullRef Killer)")]
    public static void ForceFix()
    {
        Debug.Log("Starting Force Fix...");

        GameObject player = GameObject.Find("Player_New");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError("CRITICAL: Player Not Found in Scene!");
            return;
        }

        Debug.Log($"Targeting Player: {player.name}");

        // 1. Fix ThirdPersonController References
        Component tpc = player.GetComponent("ThirdPersonController");
        if (tpc != null)
        {
            SerializedObject so = new SerializedObject(tpc);
            so.Update();

            // Fix CinemachineCameraTarget
            Transform root = player.transform.Find("PlayerCameraRoot");
            if (root == null)
            {
                GameObject r = new GameObject("PlayerCameraRoot");
                r.transform.SetParent(player.transform);
                r.transform.localPosition = new Vector3(0, 1.375f, 0);
                root = r.transform;
                Debug.Log("Created missing PlayerCameraRoot");
            }

            SerializedProperty targetProp = so.FindProperty("CinemachineCameraTarget");
            if (targetProp != null)
            {
                targetProp.objectReferenceValue = root.gameObject;
                Debug.Log("Assigned CinemachineCameraTarget");
            }
            
            // Fix Ground Layers
            SerializedProperty groundProp = so.FindProperty("GroundLayers");
            if (groundProp != null)
            {
                groundProp.intValue = LayerMask.GetMask("Default", "Terrain");
                Debug.Log("Fixed Ground Layers");
            }

            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogError("ThirdPersonController component missing!");
        }

        // 2. Fix Animator
        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            // Controller
            if (anim.runtimeAnimatorController == null || !anim.runtimeAnimatorController.name.Contains("Starter"))
            {
                string[] guids = AssetDatabase.FindAssets("StarterAssetsThirdPerson t:AnimatorController");
                if (guids.Length > 0)
                {
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    Debug.Log("Assigned StarterAssets Controller");
                }
            }

            // Avatar
            if (anim.avatar == null)
            {
                string[] guids = AssetDatabase.FindAssets("Knight t:Avatar");
                if (guids.Length > 0)
                {
                    anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    Debug.Log("Assigned Knight Avatar");
                }
            }
        }

        // 3. Mark Dirty and Save
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        
        Debug.Log("Force Fix Complete. Scene Saved.");
    }
}
