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

    [Header("UI")]
    public Image healthBar;
    public Image staminaBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void Update()
    {
        // Regenerate Stamina
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        UpdateUI();
    }

    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0);
    }

    public bool HasEnoughStamina(float amount)
    {
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
