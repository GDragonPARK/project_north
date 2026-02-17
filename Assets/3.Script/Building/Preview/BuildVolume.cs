using UnityEngine;

public class BuildVolume : MonoBehaviour
{
    public Vector3 center;
    public Vector3 size = Vector3.one;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(center, size);
        Gizmos.DrawWireCube(center, size);
    }
}
