using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float m_currentHealth;

    public UnityEvent<float, Vector3> OnDamage; // amount, hitPosition
    public UnityEvent<Vector3> OnDeath; // deathPosition

    public bool IsDead => m_currentHealth <= 0;
    public float CurrentHealth => m_currentHealth;

    private void Start()
    {
        m_currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPosition)
    {
        if (IsDead) return;

        m_currentHealth -= amount;
        OnDamage?.Invoke(amount, hitPosition);

        if (m_currentHealth <= 0)
        {
            m_currentHealth = 0;
            OnDeath?.Invoke(hitPosition);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return; // Optional: can't heal dead things? 

        m_currentHealth += amount;
        if (m_currentHealth > maxHealth) m_currentHealth = maxHealth;
    }

    public void Kill()
    {
        TakeDamage(m_currentHealth + 9999f, transform.position);
    }
}
