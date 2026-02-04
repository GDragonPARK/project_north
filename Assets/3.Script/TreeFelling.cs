using UnityEngine;

public class TreeFelling : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float m_currentHealth;

    [Header("Physics Settings")]
    public float fallForce = 15f;
    private bool m_isFelled = false;
    private Rigidbody m_rb;

    [Header("Visuals & Drops")]
    public GameObject hitParticlePrefab;
    public GameObject logPrefab;
    public int logsToSpawn = 2;

    private void Start()
    {
        m_currentHealth = maxHealth;
        
        // Rigidbody happens here just in case, but usually managed by Spawner
        m_rb = GetComponent<Rigidbody>();
        if (m_rb == null) m_rb = gameObject.AddComponent<Rigidbody>();
        
        // Static until felled
        m_rb.isKinematic = true;
        m_rb.useGravity = true;
        m_rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        if (m_isFelled) return;

        m_currentHealth -= damage;
        // Debug.Log($"Tree hit! Remaining HP: {m_currentHealth}");
        
        // Visual Feedback
        if (hitParticlePrefab)
        {
            // Pointing up or away? Simple Up for now or identity
            Instantiate(hitParticlePrefab, hitPosition, Quaternion.identity);
        }

        if (m_currentHealth <= 0)
        {
            FellTree(hitPosition);
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
        yield return new WaitForSeconds(4.0f); // Wait for fall
        
        if (logPrefab)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
            for(int i=0; i<logsToSpawn; i++)
            {
                // Spread them out slightly
                Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
                Instantiate(logPrefab, spawnPos + offset + (transform.forward * i * 1.5f), transform.rotation);
            }
        }
        
        // VFX? Smoke?
        Destroy(gameObject);
    }
    
    [ContextMenu("Test Felling")]
    public void TestFelling()
    {
        TakeDamage(maxHealth, transform.position + transform.forward);
    }
}
