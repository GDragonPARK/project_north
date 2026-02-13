using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    public enum SnapType { All, Wall, Floor, Pillar }
    public SnapType type = SnapType.All;
    
    [SerializeField] private float m_radius = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }

    public float GetRadius() => m_radius;
}
