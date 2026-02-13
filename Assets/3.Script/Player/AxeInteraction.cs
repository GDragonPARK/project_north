using UnityEngine;

public class AxeInteraction : MonoBehaviour
{
    public float damage = 50f;
    private BoxCollider m_collider;

    private void Start()
    {
        m_collider = GetComponent<BoxCollider>();
        if (m_collider == null)
        {
             m_collider = gameObject.AddComponent<BoxCollider>();
             m_collider.isTrigger = true;
        }

        // Ensure Rigidbody for Trigger events
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
             rb = gameObject.AddComponent<Rigidbody>();
             rb.isKinematic = true; 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"[Axe] Hit: {other.name}");

        ResourceNode node = other.GetComponent<ResourceNode>();
        if (node == null) node = other.GetComponentInParent<ResourceNode>();

        if (node != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 dir = (hitPoint - transform.position).normalized;
            
            Debug.Log($"[Axe] Tree Hit! Damage: {damage}");
            node.GetHit(damage, hitPoint, dir);
        }
    }
    
    // Controlled by PlayerAttacker or Animation Events
    public void EnableHit()
    {
        if(m_collider) m_collider.enabled = true;
    }

    public void DisableHit()
    {
        if(m_collider) m_collider.enabled = false;
    }
}
