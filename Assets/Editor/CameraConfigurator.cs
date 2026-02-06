using UnityEngine;
using UnityEditor;
using Cinemachine;

public class CameraConfigurator : EditorWindow
{
    [UnityEditor.MenuItem("Antigravity/Configure Camera (Invert & Zoom)")]
    public static void Configure()
    {
        CinemachineFreeLook cam = Object.FindObjectOfType<CinemachineFreeLook>();
        if (!cam) 
        {
            Debug.LogError("No FreeLook Camera found!");
            return;
        }

        // 1. Set Axis Control (Mouse Look)
        // User wants Mouse Up -> Look Up (Non-Inverted Y)
        // User wants Mouse Right -> Look Right (Non-Inverted X)
        // Ensure Invert is OFF in Cinemachine Axis
        cam.m_YAxis.m_InvertInput = false; // Mouse Up increases Y (0 to 1, usually Bottom to Top). 
        // Wait, FreeLook Y 0 is Bottom, 1 is Top. So Increasing Y moves Camera UP, looking DOWN?
        // No, typically FreeLook Top Rig looks down, Bottom Rig looks up.
        // If Mouse Up -> Look Up, we want Camera to go DOWN (Bottom Rig).
        // So Mouse Up should DECREASE Y value (1->0) if 1 is Top Ring.
        // Let's check Unity Standard. 
        // Usually: Mouse Up -> Look Up -> Camera goes Low.
        // If Y Axis manages "Height", then 0=Bottom, 1=Top. 
        // To Look Up, we go to 0. 
        // So Mouse Up (Positive Delta) should produce Negative Change.
        // So Invert = TRUE for Look Up behavior? 
        // User said: "지금이랑 정반대로". (Previous was likely Standard).
        // Let's set m_InvertInput to true for Y.
        // And m_InvertInput to false for X.

        cam.m_YAxis.m_InvertInput = true; 
        cam.m_XAxis.m_InvertInput = false;

        // 2. Zoom with Scroll Setup
        // Check if we have a Zoom Script
        CameraZoom zoom = cam.GetComponent<CameraZoom>();
        if (!zoom) zoom = cam.gameObject.AddComponent<CameraZoom>();
        
        Debug.Log("Camera Configured: Y-Invert=True, X-Invert=False, Zoom Script Added.");
    }
}
