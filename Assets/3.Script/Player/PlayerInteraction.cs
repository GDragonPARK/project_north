using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 5f;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Interact();
        }
    }

    void Interact()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            ResourceObject resource = hit.collider.GetComponent<ResourceObject>();
            if (resource != null)
            {
                if (CharacterStats.Instance != null && !CharacterStats.Instance.HasEnoughStamina(10f))
                {
                    Debug.Log("Out of stamina!");
                    return;
                }
                resource.Gather();
            }
        }
    }
}
