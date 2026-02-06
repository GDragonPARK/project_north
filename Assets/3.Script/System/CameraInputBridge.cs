using UnityEngine;
using Cinemachine;
using StarterAssets;

public class CameraInputBridge : MonoBehaviour
{
    // Simple bridge to force Cinemachine to use StarterAssets input
    // This avoids "Input Action not connected" issues with CinemachineInputProvider

    private StarterAssetsInputs _input;

    private void Awake()
    {
        // Setup Delegate
        CinemachineCore.GetInputAxis = GetAxisCustom;
    }

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        if (!_input) _input = FindObjectOfType<StarterAssetsInputs>();
    }

    private float GetAxisCustom(string axisName)
    {
        if (_input == null) return UnityEngine.Input.GetAxis(axisName);

        // Map "Mouse X" / "Mouse Y" which Cinemachine asks for by default
        // to our Input System vector
        if (axisName == "Mouse X") return _input.look.x; 
        if (axisName == "Mouse Y") return _input.look.y;

        return UnityEngine.Input.GetAxis(axisName);
    }
}
