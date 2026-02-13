using UnityEngine;
using StarterAssets;

public class PhysicsHypothesisTester : MonoBehaviour
{
    public ThirdPersonController tpc;
    public Animator animator;
    public CharacterController cc;
    public Rigidbody rb;

    [Header("Hypothesis 1: Root Motion")]
    public bool disableRootMotion = false;

    [Header("Hypothesis 2: Gravity Inversion")]
    public bool invertGravity = false;
    private float originalGravity;

    [Header("Hypothesis 3: CC Conflict")]
    public bool disableCharacterController = false;

    [Header("Monitoring")]
    public float currentY;
    public float velocityY;
    public bool isRising;

    private void Start()
    {
        tpc = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (tpc) originalGravity = tpc.Gravity;
    }

    private float _lastY;

    private void Update()
    {
        currentY = transform.position.y;
        
        // Calculate velocity manually since _verticalVelocity is private
        if (Time.deltaTime > 0)
        {
            velocityY = (currentY - _lastY) / Time.deltaTime;
        }
        _lastY = currentY;
        
        isRising = transform.position.y > 15f; // Threshold

        // 1. Root Motion
        if (animator) animator.applyRootMotion = !disableRootMotion;

        // 2. Gravity
        if (tpc)
        {
            if (invertGravity) tpc.Gravity = Mathf.Abs(originalGravity); // Force positive
            else tpc.Gravity = originalGravity; // Restore negative
        }

        // 3. CharacterController
        if (cc) cc.enabled = !disableCharacterController;
        
        // Log status if rising
        if (isRising && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HypothesisTester] Rising! Y: {currentY:F2}, RootMotion: {animator.applyRootMotion}, Gravity: {tpc.Gravity}, CC: {cc.enabled}");
        }
    }
}
