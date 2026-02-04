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
        Debug.Log($"[Weapon] Hit Trigger: {other.name} (Tag: {other.tag}) (Layer: {other.gameObject.layer})");

        // Check for TreeFelling script
        TreeFelling tree = other.GetComponent<TreeFelling>();
        if (tree == null) tree = other.GetComponentInParent<TreeFelling>();

        if (tree != null)
        {
            Debug.Log($"[Weapon] Valid Tree Hit: {tree.name}");
            tree.TakeDamage(damage, transform.position);
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