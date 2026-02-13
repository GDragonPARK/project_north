using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 10f;
    public LayerMask interactLayer;

    void Update()
    {
        CheckInteraction();

        // Attack/Interact via Mouse (Legacy, keeping for compatibility if needed)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // InteractRaycast(); // This was for ResourceObject
        }
    }

    private GameObject _lastHoveredObject;

    void CheckInteraction()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Ensure layer mask is not "Nothing"
        if (interactLayer.value == 0) interactLayer = LayerMask.GetMask("Default", "Item", "Resource");

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        bool hitSomething = false;
        GameObject currentHovered = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            // Check for ItemObject
            ItemObject item = hit.collider.GetComponent<ItemObject>();
            if (item == null) item = hit.collider.GetComponentInParent<ItemObject>();

            if (item != null)
            {
                hitSomething = true;
                currentHovered = item.gameObject;
                Debug.Log($"Item Hovered: {currentHovered.name}");

                if (InteractionUI.Instance != null) InteractionUI.Instance.Show(item.GetInteractionMessage());

                // Handle Glow
                if (_lastHoveredObject != currentHovered)
                {
                    // Remove glow from old
                    RemoveGlow(_lastHoveredObject);
                    // Add glow to new
                    AddGlow(currentHovered);
                    _lastHoveredObject = currentHovered;
                }

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    item.PickUp();
                    // Force UI update
                    if (InventorySystem.Instance != null) InventorySystem.Instance.ForceUIUpdate();
                }
            }
            // ResourceObject logic
            else
            {
                ResourceObject resource = hit.collider.GetComponent<ResourceObject>();
                if (resource != null)
                {
                    hitSomething = true;
                    // Resource interaction logic here if needed...
                }
            }
        }

        if (!hitSomething)
        {
            if (InteractionUI.Instance != null) InteractionUI.Instance.Hide();
            if (_lastHoveredObject != null)
            {
                RemoveGlow(_lastHoveredObject);
                _lastHoveredObject = null;
            }
        }
    }

    private void AddGlow(GameObject target)
    {
        if (target == null) return;
        
        // Try to add glow for visual feedback
        // Check if ChocDino.UIFX is available
        var glow = target.GetComponent<ChocDino.UIFX.GlowFilter>();
        if (glow == null) glow = target.AddComponent<ChocDino.UIFX.GlowFilter>();
        
        if (glow != null)
        {
            glow.enabled = true;
            glow.Color = Color.yellow; // Example glow color
            glow.Strength = 2.0f; // Increased strength
        }
    }

    private void RemoveGlow(GameObject target)
    {
        if (target == null) return;
        var glow = target.GetComponent<ChocDino.UIFX.GlowFilter>();
        if (glow != null)
        {
            // Keep component but disable it to avoid GC alloc on add/remove constantly
            glow.enabled = false;
        }
    }
}
