using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using Cinemachine; 
// Note: Namespace might be "Cinemachine" in older versions (2.x) or "Unity.Cinemachine" in 3.x.
// Checking package version... user has 915237edaa40 which implies custom or specific version. 
// Standard Cinemachine usually uses "Cinemachine" namespace.
// Let's try "Cinemachine" first, if it fails compilation we can fix.
#endif

namespace Cinemachine.Editor
{
    public class CameraSystemUpgrader : EditorWindow
    {
        [MenuItem("Tools/Upgrade Camera to Cinemachine")]
        public static void UpgradeCamera()
        {
            // 1. Setup Brain on Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                 mainCam = Object.FindFirstObjectByType<Camera>();
                 if (mainCam == null)
                 {
                     Debug.LogError("No Main Camera found!");
                     return;
                 }
            }

            // Check namespaces using reflection or just try adding component by string to avoid compile errors if namespace differs
            // Actually, we can just use GameObject.AddComponent
            var brain = mainCam.GetComponent("CinemachineBrain");
            if (brain == null) 
            {
                // Create CinemachineBrain if it doesn't exist
                // utilize UnityEditor.EditorApplication.ExecuteMenuItem if needed, but direct AddComponent is better
                // We'll rely on the Assembly "Cinemachine"
                Undo.AddComponent(mainCam.gameObject, GetType("Cinemachine.CinemachineBrain"));
                Debug.Log("Added CinemachineBrain to Main Camera.");
            }

            // 2. Disable Old Scripts
            // "SimpleFollowCamera", "ThirdPersonController" camera logic, etc.
            MonoBehaviour[] scripts = mainCam.GetComponents<MonoBehaviour>();
            foreach(var s in scripts)
            {
                if (s.GetType().Name.Contains("Follow") || s.GetType().Name.Contains("Simple") || s.GetType().Name.Contains("Studio"))
                {
                    s.enabled = false;
                    Debug.Log($"Disabled legacy camera script: {s.GetType().Name}");
                }
            }

            // 3. Create FreeLook Camera
            GameObject flGo = GameObject.Find("CM FreeLook1");
            if (flGo == null)
            {
                 flGo = new GameObject("CM FreeLook1");
                 Undo.RegisterCreatedObjectUndo(flGo, "Create FreeLook");
            }

            // Add CinemachineFreeLook
            var freeLook = flGo.GetComponent("CinemachineFreeLook");
            if (freeLook == null)
            {
                freeLook = Undo.AddComponent(flGo, GetType("Cinemachine.CinemachineFreeLook"));
            }

            if (freeLook != null)
            {
                // 4. Configure Target
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) player = GameObject.Find("Player");
                if (player == null) player = GameObject.Find("Player_New");

                if (player)
                {
                    Transform lookAt = player.transform.Find("PlayerCameraRoot"); 
                    if (lookAt == null) lookAt = player.transform; // Fallback

                    // Reflection allows us to set properties without hard dependency on namespace in this script context
                    // (Though normally we'd just use `using Cinemachine;`)
                    SerializedObject so = new SerializedObject(freeLook);
                    so.FindProperty("m_Follow").objectReferenceValue = lookAt;
                    so.FindProperty("m_LookAt").objectReferenceValue = lookAt;
                    
                    // Orbits
                    SerializedProperty orbits = so.FindProperty("m_Orbits");
                    if (orbits != null && orbits.arraySize >= 3)
                    {
                        // Top
                        orbits.GetArrayElementAtIndex(0).FindPropertyRelative("m_Height").floatValue = 4.5f;
                        orbits.GetArrayElementAtIndex(0).FindPropertyRelative("m_Radius").floatValue = 1.75f;
                        
                        // Middle
                        orbits.GetArrayElementAtIndex(1).FindPropertyRelative("m_Height").floatValue = 2.5f;
                        orbits.GetArrayElementAtIndex(1).FindPropertyRelative("m_Radius").floatValue = 3.0f;

                        // Bottom
                        orbits.GetArrayElementAtIndex(2).FindPropertyRelative("m_Height").floatValue = 0.4f;
                        orbits.GetArrayElementAtIndex(2).FindPropertyRelative("m_Radius").floatValue = 1.3f;
                    }
                    
                    // X Axis Speed
                    SerializedProperty xAxis = so.FindProperty("m_XAxis");
                    xAxis.FindPropertyRelative("m_MaxSpeed").floatValue = 300f;

                    // Y Axis Speed
                    SerializedProperty yAxis = so.FindProperty("m_YAxis");
                    yAxis.FindPropertyRelative("m_MaxSpeed").floatValue = 2f;

                    so.ApplyModifiedProperties();
                    Debug.Log("Configured Cinemachine FreeLook parameters.");
                }
                else
                {
                    Debug.LogError("Player not found for Camera Target!");
                }
            
                // Add Collider to prevent clipping
                var collider = flGo.GetComponent("CinemachineCollider");
                if (collider == null)
                {
                    collider = Undo.AddComponent(flGo, GetType("Cinemachine.CinemachineCollider"));
                }
                
                if (collider != null)
                {
                     // Configure Strategy: Obstacle Displace is usually good for FreeLook, 
                     // but PullCameraForward is better to avoid seeing inside walls.
                     // We need to set fields via SerializedObject or Reflection since we don't have the type at compile time easily.
                     
                     SerializedObject co = new SerializedObject(collider);
                     // m_Strategy: 0 = PullCameraForward, 1 = Preserve, ... 
                     // Let's stick to default PullCameraForward
                     
                     // Optimization: collide against Default, Ground, Walls
                     // m_CollideAgainst
                     co.FindProperty("m_CollideAgainst").intValue = LayerMask.GetMask("Default", "Terrain", "Obstacles");
                     
                     // Smoothing
                     co.FindProperty("m_Damping").floatValue = 0.2f;
                     
                     co.ApplyModifiedProperties();
                     Debug.Log("Added CinemachineCollider (Anti-Clipping).");
                }

            }
            else
            {
                Debug.LogError("Could not add CinemachineFreeLook component. Check package installation.");
            }
        }

        private static System.Type GetType(string typeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
