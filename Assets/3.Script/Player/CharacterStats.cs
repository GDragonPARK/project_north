using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterStats : MonoBehaviour
{
    public static CharacterStats Instance;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 5f;
    [Header("Survival")]
    public float sprintCostPerSec = 15f;
    public float actionCost = 10f;
    public bool isResting;
    public bool isSprinting; // Set by Controller
    public bool isExhausted;

    [Header("UI")]
    public Image healthBar;
    public Image staminaBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        // Auto-Setup UI
        if (staminaBar == null)
        {
             GameObject go = GameObject.Find("StaminaBar"); 
             if (go) staminaBar = go.GetComponent<Image>();
             if (!go)
             {
                 go = GameObject.Find("StaminaGauge"); // Try alt name
                 if (go) staminaBar = go.GetComponent<Image>();
             }
        }
    }

    void Update()
    {
        CheckRestingConditions();
        HandleRegeneration();
        UpdateUI();
    }

    private void CheckRestingConditions()
    {
        // 1. Check for Fireplace
        bool nearFire = false;
        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        foreach(var h in hits) 
            if (h.name.Contains("Fire") || h.name.Contains("Fireplace")) { nearFire = true; break; }

        // 2. Check for Floor (Raycast Down)
        bool onFloor = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2.0f, LayerMask.GetMask("Default", "Building")); 
        
        if (onFloor)
        {
             RaycastHit hit;
             if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 2.0f))
             {
                 if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain")) onFloor = false;
             }
        }

        isResting = onFloor || nearFire;
    }

    private void HandleRegeneration()
    {
        if (isSprinting) return; // No regen while sprinting

        float rate = staminaRegenRate;
        if (isResting) rate *= 3f; // 3x multiplier

        if (currentStamina < maxStamina)
        {
            currentStamina += rate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        // Exhaustion Recovery Logic
        if (isExhausted)
        {
            float recoveryThreshold = maxStamina * 0.1f; // 10%
            if (currentStamina > recoveryThreshold) isExhausted = false;
        }
    }

    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0);
        
        if (currentStamina <= 0) isExhausted = true;
    }
    
    public bool CanSprint()
    {
        return !isExhausted && currentStamina > 0;
    }

    public bool HasEnoughStamina(float amount)
    {
        if (isExhausted) return false;
        return currentStamina >= amount;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void BoostMaxStamina(float amount, float duration)
    {
        // Simple boost for now, could be timed
        maxStamina += amount;
        currentStamina += amount;
        Debug.Log($"Stamina boosted by {amount}!");
    }

    void UpdateUI()
    {
        if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;
        if (staminaBar != null) staminaBar.fillAmount = currentStamina / maxStamina;
        if (healthText != null) healthText.text = $"HP: {Mathf.CeilToInt(currentHealth)}";
        if (staminaText != null) staminaText.text = $"STAMINA: {Mathf.CeilToInt(currentStamina)}";
    }
}
