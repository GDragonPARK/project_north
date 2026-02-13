using UnityEngine;

public class AxeAdjuster : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Drag the Axe GameObject here. If empty, script tries to find 'Axe' in children.")]
    [SerializeField] private GameObject axeObject;
    
    [Tooltip("Drag the Hand Transform here. If empty, attempts to find Humanoid Left Hand.")]
    [SerializeField] private Transform handTransform;

    [Header("Adjustments")]
    [Tooltip("Local position offset relative to the hand.")]
    public Vector3 positionOffset = new Vector3(0.6f, 0.3f, -0.5f); // User specific
    [Tooltip("Local rotation offset (Euler) applied.")]
    public Vector3 rotationOffset = new Vector3(-150f, 30f, 250f); // User specific

    [Header("Alignment Settings")]
    [Tooltip("If true, overrides hand rotation. DISABLE for natural attacks.")]
    public bool forceForwardAlignment = false;
    [Tooltip("If true, uses Player/Root forward. If false, uses Camera forward.")]
    public bool usePlayerForward = true;

    private Animator animator;
    private Transform rootTransform; // Player's root transform

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        animator = GetComponent<Animator>();
        rootTransform = transform.root; // Assume script is on the player hierarchy somewhere

        // SAFETY: Try to find Hand if not assigned
        if (handTransform == null)
        {
            if (animator != null && animator.isHuman)
            {
                handTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
            if (handTransform == null) Debug.LogWarning("[AxeAdjuster] Left Hand Bone not found on Animator!");
        }

        // SAFETY: Try to find Axe object if not assigned
        if (axeObject == null)
        {
            // Try searching children strictly
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.name.Contains("Axe") || t.name.Contains("Weapon"))
                {
                    axeObject = t.gameObject;
                    Debug.Log($"[AxeAdjuster] Auto-assigned Axe Object: {t.name}");
                    break;
                }
            }
            
            if (axeObject == null) Debug.LogError("[AxeAdjuster] Axe Object is not assigned and could not be found automatically!");
        }

        // 3. Parenting
        if (axeObject != null && handTransform != null)
        {
            // Reparent to the hand so it moves with it
            axeObject.transform.SetParent(handTransform);
            Debug.Log($"[AxeAdjuster] Attached {axeObject.name} to {handTransform.name}");
        }
    }

    private void LateUpdate()
    {
        if (axeObject == null || handTransform == null) return;

        // Enforce transform to override animation keyframes if necessary.
        // Also allows real-time editing in Inspector during Play Mode.
        // USER REQUEST: Script override DISABLED. 
        // Axe now relies on purely being a child of the bone (Hierarchy).
        return; 
        
        /*
        if (axeObject != null && handTransform != null)
        {
            // 1. Position: Parented logic usually handles this, but we can enforce local offset
            axeObject.transform.position = handTransform.TransformPoint(positionOffset);
        // ... (rest commented out) } 
        */
    }

    // Helper to find object by name recursively
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
