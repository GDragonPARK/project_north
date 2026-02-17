using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 5.0f; // Increased to 5.0f for better range
    public LayerMask interactLayer;

    private StarterAssets.StarterAssetsInputs _input;
    private GameObject _lastHoveredObject;

    private void Start()
    {
        // 1. SELF-DESTRUCT IF DUPLICATE (Camera)
        if (gameObject.name == "Main Camera")
        {
            Debug.LogWarning("[PlayerInteraction] Detected on Main Camera. Self-destructing to prevent duplicate signals.");
            Destroy(this);
            return;
        }

        // 2. CONNECT INPUT (Global Search)
        _input = Object.FindAnyObjectByType<StarterAssets.StarterAssetsInputs>();
        
        if (_input == null)
        {
            Debug.LogError("[PlayerInteraction] Critical: StarterAssetsInputs component missing in Scene!");
        }
        else
        {
            Debug.Log($"[PlayerInteraction] Connected to Input on {_input.name}");
        }

        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (_input == null) return;

        // A. HOVER CHECK (Visuals only)
        HandleHover();

        // B. INTERACTION CHECK (Action)
        if (_input.interact)
        {
            Debug.Log("[PlayerInteraction] 1. Signal Received!");
            
            PerformInteraction();
            
            // RESET SIGNAL
            _input.interact = false; 
        }
        
        // C. MOUSE FALLBACK (Optional/Legacy)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // InteractRaycast(); 
        }
    }

    private void HandleHover()
    {
        if (cam == null) return;
        
        // 1. Explicit Ray from Camera Center
        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward;
        Ray ray = new Ray(origin, direction);

        // 2. Relaxed Layer Mask (Check Everything)
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance)) 
        {
            GameObject target = hit.collider.gameObject;
            
            // EXCLUDE ITEMS (Auto-Pickup handles them now)
            // Check layer 10
            if (target.layer == 10) return; 

            // Check for OTHER Interactables (e.g. Doors, Chests)
            // For now, we only had ItemObject. If we have other interactables, logic goes here.
            // Since we deleted ItemObject interaction logic, this might be empty for now.
             /*
            Interactable interactable = target.GetComponent<Interactable>();
            if (interactable != null) ...
            */
        }

        // Nothing found
        if (InteractionUI.Instance != null) InteractionUI.Instance.Hide();
        if (_lastHoveredObject != null)
        {
            RemoveVisuals(_lastHoveredObject);
            _lastHoveredObject = null;
        }
    }

    private void PerformInteraction()
    {
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward;
        Ray ray = new Ray(origin, direction);

        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            // IGNORE ITEMS
            if (hit.collider.gameObject.layer == 10) 
            {
                Debug.Log("[PlayerInteraction] Hit Item (Ignored for Auto-Pickup)");
                return;
            }

            // Hit something else?
            Debug.Log($"[PlayerInteraction] Hit: {hit.collider.name}");
        }
    }

    private void AddVisuals(GameObject target)
    {
        if (target == null) return;

        // 1. Glow
        var glow = target.GetComponent<ChocDino.UIFX.GlowFilter>();
        if (glow == null) glow = target.AddComponent<ChocDino.UIFX.GlowFilter>();
        if (glow != null)
        {
            glow.enabled = true;
            glow.Color = Color.yellow; 
            glow.Strength = 2.0f; 
        }

        // 2. Sparkle FX
        Transform fx = target.transform.Find("InteractionFX"); // Old way checks
        if (fx != null) fx.gameObject.SetActive(true);
        
        // 3. Sparkle Point (New Quad)
        Transform point = target.transform.Find("SparklePoint");
        if (point != null) point.gameObject.SetActive(true);
    }

    private void RemoveVisuals(GameObject target)
    {
        if (target == null) return;

        var glow = target.GetComponent<ChocDino.UIFX.GlowFilter>();
        if (glow != null) glow.enabled = false;

        Transform fx = target.transform.Find("InteractionFX");
        if (fx != null) fx.gameObject.SetActive(false);
        
        Transform point = target.transform.Find("SparklePoint");
        if (point != null) point.gameObject.SetActive(false);
    }
}
