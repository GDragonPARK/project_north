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

    [Header("SFX")]
    public AudioClip hitSound;   // 도끼로 찍을 때 나는 소리
    public AudioClip fallSound;  // 나무가 쓰러질 때 나는 소리

    [Header("Visuals & Drops")]
    public GameObject hitEffectPrefab;
    public string woodChipPoolTag = "WoodChip";
    public GameObject fallenTreePrefab;
    public GameObject logPrefab;

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
            ObjectPoolManager.Instance.SpawnFromPool(woodChipPoolTag, hitPosition, Quaternion.LookRotation(Vector3.up));
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (m_healthSystem)
            m_healthSystem.TakeDamage(damage, hitPoint);
    }

    /// <summary>[Phase 7.1 + Phase 8.1] 도끼 등 무기에서 호출. 3D 타격음 재생 포함.</summary>
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (m_isFelled) return;

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, hitPoint, 1.0f);

        if (m_healthSystem)
            m_healthSystem.TakeDamage(damage, hitPoint);
    }

    private void FellTree(Vector3 hitPosition)
    {
        if (m_isFelled) return;
        m_isFelled = true;
        Debug.Log("<color=orange>Tree Falling (Swap)!</color>");

        if (fallSound != null)
            AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.0f);

        if (fallenTreePrefab != null)
        {
            GameObject fallenTree = Instantiate(fallenTreePrefab, transform.position, transform.rotation);
            fallenTree.transform.localScale = transform.localScale;

            Rigidbody rb = fallenTree.GetComponent<Rigidbody>();
            if (rb == null) rb = fallenTree.AddComponent<Rigidbody>();
            rb.mass = 100f;
            rb.isKinematic = false;
            rb.useGravity = true;

            MeshCollider mc = fallenTree.GetComponent<MeshCollider>();
            if (mc == null) mc = fallenTree.AddComponent<MeshCollider>();
            mc.convex = true;

            Vector3 fallDir = (transform.position - hitPosition).normalized;
            fallDir.y = 0;
            rb.AddForceAtPosition(fallDir * fallForce, transform.position + Vector3.up * 5f, ForceMode.Impulse);
            rb.AddTorque(transform.right * fallForce * 0.5f, ForceMode.Impulse);

            FallenLog fallenLog = fallenTree.GetComponent<FallenLog>();
            if (fallenLog == null) fallenLog = fallenTree.AddComponent<FallenLog>();

            if (logPrefab != null && fallenLog.lootPrefab == null)
                fallenLog.lootPrefab = logPrefab;
        }
        else
        {
            Debug.LogError($"[TreeFelling] 'fallenTreePrefab' is not assigned on {name}! Cannot swap.");
        }

        Destroy(gameObject);
    }
}
