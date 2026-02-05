using UnityEngine;
using UnityEditor;
using Cinemachine;

public class CameraCollisionSetup : EditorWindow
{
    [MenuItem("Tools/Fix Camera Clipping")]
    public static void Fix()
    {
        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam)
        {
            var collider = vcam.GetComponent<CinemachineCollider>();
            if (!collider) 
            {
                // In standard Unity/Cinemachine, Extension is a Component on the same GO
                collider = vcam.gameObject.AddComponent<CinemachineCollider>();
                Debug.Log("Added CinemachineCollider component.");
            }
            
            // Set Layers
            collider.m_CollideAgainst = (1 << 0) | (1 << 8) | (1 << 9); 
            collider.m_Strategy = CinemachineCollider.ResolutionStrategy.PreserveCameraHeight;
            collider.m_Damping = 0.2f;
            
            Debug.Log($"Camera Collider Configured on {vcam.name}");
        }
        else
        {
            Debug.LogError("No CinemachineVirtualCamera found!");
        }
    }
}