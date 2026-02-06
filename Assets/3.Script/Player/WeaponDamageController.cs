using UnityEngine;

public class WeaponDamageController : MonoBehaviour
{
    public float damage = 20f;
    public ToolType toolType = ToolType.Axe; // Default to Axe for existing prefab
    private BoxCollider m_collider;

    private void Start()
    {
        m_collider = GetComponent<BoxCollider>();
        if (m_collider) m_collider.enabled = false; // Disabled by default
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"[Weapon] Hit Trigger: {other.name} (Tag: {other.tag}) (Layer: {other.gameObject.layer})");

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // Priority 0: ResourceObject (New System)
        ResourceObject res = other.GetComponent<ResourceObject>();
        if (res == null) res = other.GetComponentInParent<ResourceObject>();
        
        if (res != null)
        {
            res.Gather(toolType);
            // Visual Effect?
            return;
        }

        // Priority 1: HealthSystem
        HealthSystem health = other.GetComponent<HealthSystem>();
        if (health == null) health = other.GetComponentInParent<HealthSystem>();

        if (health != null)
        {
            health.TakeDamage(damage, hitPoint);
            return;
        }

        // Priority 2: Legacy TreeFelling
        TreeFelling tree = other.GetComponent<TreeFelling>();
        if (tree == null) tree = other.GetComponentInParent<TreeFelling>();

        if (tree != null)
        {
            // Legacy tree might not have ResourceObject yet
            // If it's a Tree and we are Axe, allow it.
            if (toolType == ToolType.Axe)
                tree.TakeDamage(damage, hitPoint);
            else
                Debug.Log("Wrong tool for this tree!");
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