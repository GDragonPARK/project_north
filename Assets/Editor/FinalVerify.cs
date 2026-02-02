using UnityEngine;
using UnityEditor;
using StarterAssets;
// using Cinemachine; // Might not be available in assembly here, use reflection or GameObject search

public class FinalVerify
{
    [MenuItem("Tools/Final Verify")]
    public static void Verify()
    {
        // 1. Check Player_New
        GameObject p = GameObject.Find("Player_New");
        if (p)
        {
            Debug.Log("VERIFY: Player_New found.");
            if (p.GetComponent<CharacterController>()) Debug.Log(" - CC Found.");
            if (p.GetComponent<Animator>()) Debug.Log(" - Animator Found.");
            if (p.GetComponent<ThirdPersonController>()) Debug.Log(" - ThirdPersonController Found.");
            if (p.GetComponent<Equipment_Manager>()) Debug.Log(" - Equipment_Manager Found.");
            
            // 2. Check Socket
            Transform socket = FindDeepChild(p.transform, "Weapon_Socket");
            if (socket) Debug.Log(" - Weapon_Socket Found.");
            else Debug.LogError(" - Weapon_Socket MISSING.");
        }
        else Debug.LogError("VERIFY: Player_New NOT FOUND.");

        // 3. Check Duplicate Managers
        Equipment_Manager[] managers = Object.FindObjectsByType<Equipment_Manager>(FindObjectsSortMode.None);
        Debug.Log($"VERIFY: Found {managers.Length} Equipment_Manager(s).");
        foreach(var m in managers)
        {
            Debug.Log($" - On GameObject: {m.gameObject.name}");
            if (m.gameObject.name != "Player_New")
            {
                Debug.LogWarning($"   -> Potential DUPLICATE on {m.gameObject.name}. Recommended to remove.");
                // Optional: DestroyImmediate(m);
            }
        }
        
        // 4. Check Camera
        // Assuming CinemachineVirtualCamera component logic through property checks
        GameObject vCam = GameObject.Find("PlayerFollowCamera");
        if (vCam)
        {
             SerializedObject so = new SerializedObject(vCam.GetComponent("CinemachineVirtualCamera"));
             Transform follow = so.FindProperty("m_Follow").objectReferenceValue as Transform;
             if (follow && follow.root.name == "Player_New") Debug.Log("VERIFY: Camera Following Player_New.");
             else Debug.LogWarning($"VERIFY: Camera Follow Target is {follow?.name} (Root: {follow?.root.name}). Expected Player_New child.");
        }
    }

    private static Transform FindDeepChild(Transform aParent, string aName)
    {
        foreach(Transform child in aParent)
        {
            if(child.name == aName) return child;
            var result = FindDeepChild(child, aName);
            if (result != null) return result;
        }
        return null;
    }
}