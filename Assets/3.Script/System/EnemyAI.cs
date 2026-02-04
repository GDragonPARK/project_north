using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Wander, Chase, Attack, Dead }
    
    [Header("State Settings")]
    public AIState currentState = AIState.Idle;
    public float detectRange = 10f;
    public float attackRange = 2f;
    public float wanderRadius = 15f;
    
    [Header("Health & Combat")]
    public float maxHealth = 50f;
    private float m_currentHealth;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;
    private float m_nextAttackTime;

    [Header("Loot")]
    public ItemData lootItem;
    public int minLoot = 1;
    public int maxLoot = 2;

    private NavMeshAgent m_agent;
    private Transform m_player;
    private Vector3 m_startPosition;
    private float m_stateTimer;

    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_currentHealth = maxHealth;
        m_startPosition = transform.position;
    }

    private void Start()
    {
        m_player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (currentState == AIState.Dead) return;

        float distanceToPlayer = m_player != null ? Vector3.Distance(transform.position, m_player.position) : float.MaxValue;
        
        // Dynamic stats based on time
        float currentDetectRange = detectRange;
        float currentDamage = attackDamage;

        if (TimeManager.Instance != null && TimeManager.Instance.isNight)
        {
            currentDetectRange *= 1.5f; // Night vision!
            currentDamage *= 1.25f;    // Night fury!
        }

        switch (currentState)
        {
            case AIState.Idle:
                HandleIdle(distanceToPlayer, currentDetectRange);
                break;
            case AIState.Wander:
                HandleWander(distanceToPlayer, currentDetectRange);
                break;
            case AIState.Chase:
                HandleChase(distanceToPlayer, currentDetectRange);
                break;
            case AIState.Attack:
                HandleAttack(distanceToPlayer, currentDamage);
                break;
        }
    }

    private void HandleIdle(float distanceToPlayer, float range)
    {
        if (distanceToPlayer <= range)
        {
            currentState = AIState.Chase;
            return;
        }

        m_stateTimer -= Time.deltaTime;
        if (m_stateTimer <= 0)
        {
            SetRandomWanderDestination();
            currentState = AIState.Wander;
        }
    }

    private void HandleWander(float distanceToPlayer, float range)
    {
        if (distanceToPlayer <= range)
        {
            currentState = AIState.Chase;
            return;
        }

        if (!m_agent.pathPending && m_agent.remainingDistance < 0.5f)
        {
            m_stateTimer = Random.Range(2f, 5f);
            currentState = AIState.Idle;
        }
    }

    private void HandleChase(float distanceToPlayer, float range)
    {
        if (distanceToPlayer > range * 1.5f)
        {
            currentState = AIState.Idle;
            m_agent.ResetPath();
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            currentState = AIState.Attack;
            m_agent.ResetPath();
            return;
        }

        m_agent.SetDestination(m_player.position);
    }

    private void HandleAttack(float distanceToPlayer, float damage)
    {
        if (distanceToPlayer > attackRange * 1.2f)
        {
            currentState = AIState.Chase;
            return;
        }

        if (Time.time >= m_nextAttackTime)
        {
            Debug.Log($"<color=red>{gameObject.name} Attacks Player with {damage} damage!</color>");
            m_nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void SetRandomWanderDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += m_startPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, 1))
        {
            m_agent.SetDestination(hit.position);
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentState == AIState.Dead) return;

        m_currentHealth -= damage;
        Debug.Log($"<color=yellow>{gameObject.name} took {damage} damage! HP: {m_currentHealth}</color>");

        if (currentState == AIState.Idle || currentState == AIState.Wander)
        {
            currentState = AIState.Chase; // Aggro on hit
        }

        if (m_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentState = AIState.Dead;
        m_agent.isStopped = true;
        Debug.Log($"<color=black>{gameObject.name} Died!</color>");

        // Drop Loot
        if (lootItem != null)
        {
            int count = Random.Range(minLoot, maxLoot + 1);
            Debug.Log($"<color=green>Dropped {count}x {lootItem.itemName}</color>");
            // Instantiate loot prefab or add directly for simplicity in prototype
        }

        Destroy(gameObject, 2f);
    }
}
