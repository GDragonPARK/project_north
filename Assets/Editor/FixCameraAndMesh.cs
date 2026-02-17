using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
// using Unity.Cinemachine; // Unity 6 / CMV3 - Invalid
using Cinemachine; // Legacy / Default Package
#endif

public class FixCameraAndMesh : EditorWindow
{
    [MenuItem("Antigravity/Diagnose Player Visibility")]
    public static void Diagnose()
    {
        Debug.Log("--- Starting Player Diagnosis ---");

        // 1. Find Player
        GameObject player = GameObject.Find("Player_New");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("CRITICAL: Player object not found anywhere!");
            return;
        }
        Debug.Log($"[Player] Found '{player.name}' at {player.transform.position}. Active: {player.activeInHierarchy}");

        // 2. Check Visuals
        Renderer[] rends = player.GetComponentsInChildren<Renderer>(true); // Include inactive
        if (rends.Length == 0)
        {
            Debug.LogError("[Player] No Renderers found in player children! Model missing?");
        }
        else
        {
            int enabledCount = 0;
            foreach (var r in rends)
            {
                if (r.enabled && r.gameObject.activeInHierarchy) enabledCount++;
                Debug.Log($"[Visual] Renderer '{r.name}' - Enabled: {r.enabled}, GO Active: {r.gameObject.activeInHierarchy}, Bounds Center: {r.bounds.center}");
                
                // Fix if disabled
                if (!r.enabled) 
                {
                    r.enabled = true;
                    Debug.Log($"[Fix] Enabled renderer on {r.name}");
                }
                if (!r.gameObject.activeSelf)
                {
                    r.gameObject.SetActive(true);
                    Debug.Log($"[Fix] Activated gameobject {r.name}");
                }
            }
            if (enabledCount == 0) Debug.LogWarning("[Player] All renderers were OFF. Attempted to enable them.");
        }

        // 3. Check Camera
        CinemachineFreeLook vcam = Object.FindFirstObjectByType<CinemachineFreeLook>();
        if (vcam != null)
        {
            Debug.Log($"[Camera] Found CM FreeLook '{vcam.name}'");
            bool changed = false;
            
            if (vcam.Follow != player.transform)
            {
                Debug.LogWarning($"[Camera] Follow was '{vcam.Follow?.name}', setting to '{player.name}'");
                vcam.Follow = player.transform;
                changed = true;
            }
            if (vcam.LookAt != player.transform)
            {
                Debug.LogWarning($"[Camera] LookAt was '{vcam.LookAt?.name}', setting to '{player.name}'");
                vcam.LookAt = player.transform;
                changed = true;
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(vcam);
                Debug.Log("[Fix] Camera targets updated.");
            }
        }
        else
        {
            // Try VirtualCamera
            CinemachineVirtualCamera vcam2 = Object.FindFirstObjectByType<CinemachineVirtualCamera>();
            if (vcam2 != null)
            {
                Debug.Log($"[Camera] Found VirtualCamera '{vcam2.name}'");
                 bool changed = false;
                if (vcam2.Follow != player.transform)
                {
                     vcam2.Follow = player.transform;
                     changed = true;
                }
                if (vcam2.LookAt != player.transform)
                {
                     vcam2.LookAt = player.transform;
                     changed = true;
                }
                 if (changed) EditorUtility.SetDirty(vcam2);
            }
            else
            {
                Debug.LogError("[Camera] No Cinemachine Camera found!");
            }
        }

        // 4. Check Layer Culling
        Camera mainCam = Camera.main;
        var pLayer = player.layer;
        if (mainCam)
        {
            if ((mainCam.cullingMask & (1 << pLayer)) == 0)
            {
                Debug.LogError($"[Camera] Main Camera is CULLING the player's layer ({pLayer})!");
                mainCam.cullingMask |= (1 << pLayer); // Fix
                Debug.Log("[Fix] Added player layer to Camera Culling Mask.");
            }
        }

        Debug.Log("--- Diagnosis Complete ---");
    }
}
