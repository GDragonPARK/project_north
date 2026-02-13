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
    public GameObject fallenTreePrefab; // The prefab to swap to
    public GameObject logPrefab; // Legacy field for WoodcuttingSetupV2 compatibility

    
    // Optional: If you still want logs from the fallen tree itself, 
    // but the user plan says the *new* fallen tree will handle loot via FallenTreeLoot.cs
    // We will keep this for backward compatibility or direct spawning if needed, 
    // but mainly the new prefab should have the logic.

    private void Start()
    {
        m_healthSystem = GetComponent<HealthSystem>();
        if (m_healthSystem == null)
        {
            m_healthSystem = gameObject.AddComponent<HealthSystem>();
            m_healthSystem.maxHealth = 100f; 
        }
        
        if (m_healthSystem)
        {
            m_healthSystem.OnDamage.AddListener(OnTreeHit);
            m_healthSystem.OnDeath.AddListener(FellTree);
        }

        // Standing tree does NOT need a Rigidbody or Convex MeshCollider usually, 
        // as it is static terrain detail until fell.
        // We leave it as is or ensure it's static.
    }

    private void OnDestroy()
    {
        if (m_healthSystem)
        {
            m_healthSystem.OnDamage.RemoveListener(OnTreeHit);
            m_healthSystem.OnDeath.RemoveListener(FellTree);
        }
    }

    private void OnTreeHit(float damage, Vector3 hitPosition)
    {
        if (m_isFelled) return;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnFromPool(woodChipPoolTag, hitPosition, Quaternion.LookRotation(Vector3.up));
        }
    }

    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        if (m_healthSystem)
        {
            m_healthSystem.TakeDamage(damage, hitPosition);
        }
    }

    private void FellTree(Vector3 hitPosition)
    {
        if (m_isFelled) return;
        m_isFelled = true;
        Debug.Log("<color=orange>Tree Falling (Swap)!</color>");

        if (fallenTreePrefab != null)
        {
            // 1. Swap Model
            // Instantiate the fallen version at the exact same transform
            GameObject fallenTree = Instantiate(fallenTreePrefab, transform.position, transform.rotation);
            
            // 2. Scale Inheritance
            fallenTree.transform.localScale = transform.localScale;

            // 3. Physics Setup (Ensure it falls)
            Rigidbody rb = fallenTree.GetComponent<Rigidbody>();
            if (rb == null) rb = fallenTree.AddComponent<Rigidbody>();
            rb.mass = 100f; // Reasonable weight
            rb.isKinematic = false;
            rb.useGravity = true;

            MeshCollider mc = fallenTree.GetComponent<MeshCollider>();
            if (mc == null) mc = fallenTree.AddComponent<MeshCollider>();
            mc.convex = true; // Essential for physics
            
            // 4. Force Application
            Vector3 fallDir = (transform.position - hitPosition).normalized;
            fallDir.y = 0; 
            rb.AddForceAtPosition(fallDir * fallForce, transform.position + Vector3.up * 5f, ForceMode.Impulse);
            rb.AddTorque(transform.right * fallForce * 0.5f, ForceMode.Impulse);

            // 5. Loot & Lifecycle
            // Attach loot script if missing
            if (fallenTree.GetComponent<FallenTreeLoot>() == null)
            {
                fallenTree.AddComponent<FallenTreeLoot>();
            }
        }
        else
        {
            Debug.LogError($"[TreeFelling] 'fallenTreePrefab' is not assigned on {name}! Cannot swap.");
        }

        // Destroy the static standing tree
        Destroy(gameObject);
    }
}
