using UnityEngine;

public class WeaponDamageController : MonoBehaviour
{
    public float damage = 20f;
    private BoxCollider m_collider;

    private void Start()
    {
        m_collider = GetComponent<BoxCollider>();
        if (m_collider) m_collider.enabled = false; // Disabled by default
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"[Weapon] Hit Trigger: {other.name} (Tag: {other.tag}) (Layer: {other.gameObject.layer})");

        // Use ClosestPoint for better precision if possible, but fallback to Transform position for now as requested
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // Priority 1: HealthSystem
        HealthSystem health = other.GetComponent<HealthSystem>();
        if (health == null) health = other.GetComponentInParent<HealthSystem>();

        if (health != null)
        {
            health.TakeDamage(damage, hitPoint);
            return;
        }

        // Priority 2: Legacy TreeFelling (if any remain without HealthSystem, though our new TreeFelling requires it)
        TreeFelling tree = other.GetComponent<TreeFelling>();
        if (tree == null) tree = other.GetComponentInParent<TreeFelling>();

        if (tree != null)
        {
            tree.TakeDamage(damage, hitPoint);
        }
    }

    public void EnableDamage()
    {
        if (m_collider) m_collider.enabled = true;
    }

    public void DisableDamage()
    {
        if (m_collider) m_collider.enabled = false;
    }
}