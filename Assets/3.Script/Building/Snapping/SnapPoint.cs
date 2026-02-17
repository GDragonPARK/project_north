using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    public string socketId; // ID (e.g., "Floor_N", "Wall_Bottom")
    public SnapType snapType;
    public float snapRadius = 0.25f;
    public bool isOccupied = false;
    
    // Simple array for now, can be Flags later
    public SnapType[] compatibleTypes;

    public bool CanConnectTo(SnapPoint other)
    {
        if (other == null) return false;
        
        // Basic compatibility check
        if (compatibleTypes == null || compatibleTypes.Length == 0) return true; 
        
        foreach (var type in compatibleTypes)
        {
            if (type == other.snapType) return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
        // Arrow head
        Vector3 right = transform.right * 0.1f;
        Vector3 tip = transform.position + transform.forward * 0.5f;
        Gizmos.DrawLine(tip, tip - transform.forward * 0.2f + right);
        Gizmos.DrawLine(tip, tip - transform.forward * 0.2f - right);
    }
}
