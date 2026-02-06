using UnityEngine;
using Cinemachine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float minRadius = 2f;
    public float maxRadius = 15f;

    private CinemachineFreeLook freeLook;
    private float targetRadius = 10f;

    void Start()
    {
        freeLook = GetComponent<CinemachineFreeLook>();
        if(freeLook) targetRadius = freeLook.m_Orbits[1].m_Radius; // Mid Rig
    }

    void Update()
    {
        if (!freeLook) return;
        if (UnityEngine.InputSystem.Mouse.current == null) return;

        float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Scroll Up (Positive) -> Zoom In (Decrease Radius)
            targetRadius -= Mathf.Sign(scroll) * zoomSpeed;
            targetRadius = Mathf.Clamp(targetRadius, minRadius, maxRadius);

            // Update Orbits proportionally
            freeLook.m_Orbits[0].m_Radius = targetRadius * 0.5f; // Top
            freeLook.m_Orbits[1].m_Radius = targetRadius;        // Mid
            freeLook.m_Orbits[2].m_Radius = targetRadius * 0.5f; // Bot
        }
    }
}
