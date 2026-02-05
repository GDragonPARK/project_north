using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HealthSystem))]
public class TreeFelling : MonoBehaviour
{
    private HealthSystem m_healthSystem;

    [Header("Physics Settings")]
    public float fallForce = 15f;
    private bool m_isFelled = false;
    private Rigidbody m_rb;

    [Header("Visuals & Drops")]
    public string woodChipPoolTag = "WoodChip"; // Tag for ObjectPool
    public GameObject logPrefab;
    public int logsToSpawn = 2;

    private void Start()
    {
        m_healthSystem = GetComponent<HealthSystem>();
        if (m_healthSystem == null)
        {
            m_healthSystem = gameObject.AddComponent<HealthSystem>();
            // Initialize default values if needed
            m_healthSystem.maxHealth = 100f; 
        }
        
        // Subscribe to HealthSystem Events
        if (m_healthSystem)
        {
            m_healthSystem.OnDamage.AddListener(OnTreeHit);
            m_healthSystem.OnDeath.AddListener(FellTree);
        }

        // Rigidbody happens here just in case
        m_rb = GetComponent<Rigidbody>();
        if (m_rb == null) m_rb = gameObject.AddComponent<Rigidbody>();
        
        // Static until felled
        m_rb.isKinematic = true;
        m_rb.useGravity = true;
        m_rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnDestroy()
    {
        if (m_healthSystem)
        {
            m_healthSystem.OnDamage.RemoveListener(OnTreeHit);
            m_healthSystem.OnDeath.RemoveListener(FellTree);
        }
    }

    // Called via HealthSystem Event
    private void OnTreeHit(float damage, Vector3 hitPosition)
    {
        if (m_isFelled) return;

        // Visual Feedback via Object Pool
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnFromPool(woodChipPoolTag, hitPosition, Quaternion.LookRotation(Vector3.up));
        }
    }

    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        // Wrapper for compatibility if internal calls exist, or forward to HealthSystem
        if (m_healthSystem)
        {
            m_healthSystem.TakeDamage(damage, hitPosition);
        }
    }

    private void FellTree(Vector3 hitPosition)
    {
        if (m_isFelled) return;
        m_isFelled = true;
        Debug.Log("<color=orange>Tree Falling!</color>");

        // Convex Fix
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc) mc.convex = true;

        m_rb.isKinematic = false;

        // Force
        Vector3 fallDir = (transform.position - hitPosition).normalized;
        fallDir.y = 0; 
        m_rb.AddForceAtPosition(fallDir * fallForce, transform.position + Vector3.up * 5f, ForceMode.Impulse);
        m_rb.AddTorque(transform.right * fallForce * 0.5f, ForceMode.Impulse);

        // Destruction Sequence
        StartCoroutine(DestroySequence());
    }

    System.Collections.IEnumerator DestroySequence()
    {
        // Wait for initial push
        yield return new WaitForSeconds(1.0f);

        // Wait until physics settles
        float timer = 0f;
        while (timer < 10f) // Max 10s wait
        {
            if (m_rb.linearVelocity.sqrMagnitude < 0.1f && m_rb.angularVelocity.sqrMagnitude < 0.1f)
            {
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Spawn Logs
        if (logPrefab)
        {
            // Assuming the tree is Y-up, so when fallen, the trunk aligns with transform.up
            // We iterate along the trunk to place logs
            for (int i = 0; i < logsToSpawn; i++)
            {
                // Calculate a position along the trunk
                // Start a bit up/along the tree (e.g. 1m + i*dist)
                Vector3 trunkOffset = transform.up * (1.0f + (i * 2.0f)); 
                Vector3 estimatePos = transform.position + trunkOffset;

                // Raycast to find ground
                Vector3 groundPos = estimatePos;
                if (Physics.Raycast(estimatePos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default", "Terrain")))
                {
                    groundPos = hit.point + Vector3.up * 0.2f; // Slight offset to not clip inside ground
                }
                else
                {
                    // Fallback if raycast fails (e.g. falling into void)
                    groundPos = estimatePos; 
                }

                // Random rotation for natural look, or align with tree? 
                // Aligning with tree is safer for visual continuity, but maybe randomize roll
                Quaternion logRot = transform.rotation * Quaternion.Euler(0, 0, Random.Range(0, 360));

                Instantiate(logPrefab, groundPos, logRot);
            }
        }
        else
        {
            Debug.LogWarning($"[TreeFelling] Missing 'logPrefab' on {name}. Tree destroyed without loot.");
        }
        
        Destroy(gameObject);
    }
}
