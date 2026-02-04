using System;
using UnityEngine;
using UnityEngine.InputSystem;

// [RequireComponent(typeof(Rigidbody))]
public class MyPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_walkSpeed = 5f;
    [SerializeField] private float m_runSpeed = 8f;
    [SerializeField] private float m_jumpForce = 5f;
    [SerializeField] private float m_rotationSpeed = 10f;

    [Header("Health & Stamina Stats")]
    [SerializeField] private float m_baseMaxHealth = 25f;
    [SerializeField] private float m_baseMaxStamina = 50f;
    [SerializeField] private float m_staminaRegenRate = 10f;
    [SerializeField] private float m_runStaminaCost = 15f;
    [SerializeField] private float m_attackStaminaCost = 20f;
    
    private float m_currentHealth;
    private float m_currentStamina;

    // Properties for dynamic calculation (Base + Food Bonuses)
    public float MaxHealth => m_baseMaxHealth + (FoodSystem.Instance != null ? FoodSystem.Instance.GetTotalHealthBonus() : 0);
    public float MaxStamina => m_baseMaxStamina + (FoodSystem.Instance != null ? FoodSystem.Instance.GetTotalStaminaBonus() : 0);

    [Header("Item & Attack Settings")]
    public ItemData currentItem;
    [SerializeField] private float m_defaultAttackRange = 2.5f;
    [SerializeField] private float m_defaultAttackDamage = 10f;
    [SerializeField] private float m_defaultStaminaCost = 15f;
    [SerializeField] private LayerMask m_attackLayer;
    [SerializeField] private LayerMask m_interactLayer;
    [SerializeField] private float m_interactRange = 3f;

    [Header("Ground Check")]
    [SerializeField] private float m_groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask m_groundLayer;

    private Rigidbody m_rigidbody;
    private Animator m_animator;
    private Vector2 m_moveInput;
    private bool m_isRunning;
    private bool m_isGrounded;

    // References for New Input System
    private PlayerInput m_playerInput;

    private void Awake()
    {
        /*
        m_rigidbody = GetComponent<Rigidbody>();
        m_rigidbody.freezeRotation = true;
        */
        m_animator = GetComponent<Animator>();
    }

    [Header("Environment Status")]
    public bool isWet;
    public bool isCold;
    public bool isSheltered;

    private void Update()
    {
        CheckGround();
        CheckEnvironment(); // New environmental check
        HandleStamina();
        // UpdateAnimations();
        
        // Clamp current stats to new dynamic max (e.g. as food wears off)
        m_currentHealth = Mathf.Min(m_currentHealth, MaxHealth);
        m_currentStamina = Mathf.Min(m_currentStamina, MaxStamina);

        HandleHealthRegen();
    }

    private void CheckEnvironment()
    {
        // 1. Shelter Check (Raycast up for roof)
        isSheltered = Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.up, 10f, LayerMask.GetMask("Building", "Default"));

        // 2. Wet Check
        bool isRaining = WeatherManager.Instance != null && 
                        (WeatherManager.Instance.currentWeather == WeatherManager.WeatherType.Rain || 
                         WeatherManager.Instance.currentWeather == WeatherManager.WeatherType.Storm);
        
        if (isRaining && !isSheltered) isWet = true;
        
        // Dry off if sheltered or near fire
        if (isSheltered || Fireplace.IsNearFire(transform.position))
        {
            if (isWet)
            {
                // Gradually dry off or instant? Valheim takes a moment but usually Shelter stops the drip.
                isWet = false; 
            }
        }

        // 3. Cold Check
        bool isNight = TimeManager.Instance != null && TimeManager.Instance.isNight;
        isCold = (isNight || isRaining) && !Fireplace.IsNearFire(transform.position) && !isSheltered;
    }

    private void HandleHealthRegen()
    {
        // Base regen affected by Cold
        float regenMultiplier = isCold ? 0.5f : 1.0f;
        float baseRegen = 0.5f; // Small natural regen
        
        m_currentHealth += baseRegen * regenMultiplier * Time.deltaTime;
        m_currentHealth = Mathf.Min(m_currentHealth, MaxHealth);
    }

    private void HandleStamina()
    {
        float regenMultiplier = 1.0f;
        if (isWet) regenMultiplier *= 0.75f; // -25% regen
        if (isCold) regenMultiplier *= 0.75f; // Cold also impacts stamina in Valheim (or regen)

        if (m_isRunning && m_moveInput.magnitude > 0.1f && m_isGrounded)
        {
            m_currentStamina -= m_runStaminaCost * Time.deltaTime;
        }
        else
        {
            m_currentStamina += m_staminaRegenRate * regenMultiplier * Time.deltaTime;
        }
        
        m_currentStamina = Mathf.Clamp(m_currentStamina, 0, MaxStamina);
    }

    /* 
    // CONFLICT: Handled by ThirdPersonController
    private void UpdateAnimations()
    {
        if (m_animator == null) return;

        float animSpeedMultiplier = (m_isRunning && m_currentStamina > 0) ? 2f : 1f;
        m_animator.SetFloat("forward_speed", m_moveInput.y * animSpeedMultiplier);
        m_animator.SetFloat("sideway_speed", m_moveInput.x * animSpeedMultiplier);
        m_animator.SetBool("is_grounded", m_isGrounded);
    }

    private void FixedUpdate()
    {
        // Move(); // Disable RB movement
    }
    */

    /*
    private void Move()
    {
        float targetSpeed = (m_isRunning && m_currentStamina > 0 && m_moveInput.magnitude > 0.1f) ? m_runSpeed : m_walkSpeed;
        
        Vector3 moveDir = transform.forward * m_moveInput.y + transform.right * m_moveInput.x;
        moveDir.Normalize();

        Vector3 targetVelocity = moveDir * targetSpeed;
        targetVelocity.y = m_rigidbody.linearVelocity.y;

        m_rigidbody.linearVelocity = targetVelocity;
    }
    */

    /*
    private void Jump()
    {
        m_rigidbody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
    }
    */

    private void CheckGround()
    {
        m_isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, m_groundCheckDistance + 0.1f, m_groundLayer);
    }

    // Public getters for UI
    public float GetStaminaNormalized() => m_currentStamina / MaxStamina;
    public float GetHealthNormalized() => m_currentHealth / MaxHealth;
}
