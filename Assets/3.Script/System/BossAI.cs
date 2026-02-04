using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Charge, AreaAttack, Dead }
    public BossState currentState = BossState.Idle;

    [Header("Stats")]
    public string bossName = "Eikthyr";
    public float maxHealth = 500f;
    private float m_currentHealth;
    
    [Header("Combat Settings")]
    public float detectRange = 25f;
    public float attackRange = 3f;
    public float chargeRange = 12f;
    public float areaAttackRange = 6f;

    [Header("Abilities")]
    public float chargeCooldown = 8f;
    public float areaAttackCooldown = 12f;
    private float m_nextChargeTime;
    private float m_nextAreaTime;

    private NavMeshAgent m_agent;
    private Transform m_player;
    private Animator m_animator;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnBossDeath;

    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();
        m_currentHealth = maxHealth;
    }

    private void Start()
    {
        m_player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (BossUI.Instance != null) BossUI.Instance.ShowBossUI(this);
    }

    private void Update()
    {
        if (currentState == BossState.Dead) return;

        float distance = Vector3.Distance(transform.position, m_player.position);

        switch (currentState)
        {
            case BossState.Idle:
                if (distance < detectRange) currentState = BossState.Chase;
                break;
            case BossState.Chase:
                HandleChase(distance);
                break;
            case BossState.Charge:
                // Handled via Coroutine
                break;
            case BossState.AreaAttack:
                // Handled via Coroutine
                break;
        }
    }

    private void HandleChase(float distance)
    {
        m_agent.SetDestination(m_player.position);

        // Logic to switch to special attacks
        if (distance > 5f && distance < chargeRange && Time.time > m_nextChargeTime)
        {
            StartCoroutine(ChargeRoutine());
        }
        else if (distance < areaAttackRange && Time.time > m_nextAreaTime)
        {
            StartCoroutine(AreaAttackRoutine());
        }
    }

    private IEnumerator ChargeRoutine()
    {
        currentState = BossState.Charge;
        m_nextChargeTime = Time.time + chargeCooldown;
        m_agent.isStopped = true;
        
        Debug.Log($"<color=red>{bossName} is Preparing CHARGE!</color>");
        yield return new WaitForSeconds(1.5f); // Tell time

        Vector3 chargeDir = (m_player.position - transform.position).normalized;
        float chargeSpeed = 15f;
        float chargeTime = 1f;

        while (chargeTime > 0)
        {
            transform.Translate(chargeDir * chargeSpeed * Time.deltaTime, Space.World);
            chargeTime -= Time.deltaTime;
            yield return null;
        }

        m_agent.isStopped = false;
        currentState = BossState.Chase;
    }

    private IEnumerator AreaAttackRoutine()
    {
        currentState = BossState.AreaAttack;
        m_nextAreaTime = Time.time + areaAttackCooldown;
        m_agent.isStopped = true;

        Debug.Log($"<color=blue>{bossName} uses area LIGHTNING!</color>");
        yield return new WaitForSeconds(2f); // Slam down

        // Damage logic for nearby player
        if (Vector3.Distance(transform.position, m_player.position) < areaAttackRange)
        {
            Debug.Log("<color=red>Player hit by Lightning!</color>");
        }

        m_agent.isStopped = false;
        currentState = BossState.Chase;
    }

    public void TakeDamage(float damage)
    {
        if (currentState == BossState.Dead) return;

        m_currentHealth -= damage;
        OnHealthChanged?.Invoke(m_currentHealth, maxHealth);

        if (m_currentHealth <= 0) Die();
    }

    private void Die()
    {
        currentState = BossState.Dead;
        m_agent.isStopped = true;
        OnBossDeath?.Invoke();
        Debug.Log($"<color=gold>{bossName} Defeated!</color>");
        
        // Loot Drop
        Destroy(gameObject, 5f);
    }
}
