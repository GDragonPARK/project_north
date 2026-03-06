using UnityEngine;
using StarterAssets;


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
        // [Phase 9.2] 공격 중이 아니면 데미지 무시
        var controller = GetComponentInParent<ThirdPersonController>();
        if (controller != null && !controller.isAttacking) return;

        ResourceNode node = other.GetComponent<ResourceNode>();
        if (node == null) node = other.GetComponentInParent<ResourceNode>();

        if (node != null)
        {
            Vector3 hitPoint;
            if (other is MeshCollider mc && !mc.convex)
                hitPoint = other.bounds.ClosestPoint(transform.position);
            else
                hitPoint = other.ClosestPoint(transform.position);

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
