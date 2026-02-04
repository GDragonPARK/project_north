using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    public enum MarkerType { Player, Building, Altar, Boss }
    public MarkerType markerType;
    public GameObject iconPrefab;
    
    // The actual icon instance in the world (on a specific layer)
    private GameObject m_iconInstance;
    
    private void Start()
    {
        if (iconPrefab != null)
        {
            m_iconInstance = Instantiate(iconPrefab, transform.position + Vector3.up * 50f, Quaternion.identity, transform);
            // Assign to a "Minimap" layer (user should create this layer)
            m_iconInstance.layer = LayerMask.NameToLayer("Minimap");
            
            // If it's the player, we might want it to follow rotation
            if (markerType == MarkerType.Player)
            {
                // Logic to sync rotation simplified: just leave it as child or sync in Update
            }
            else
            {
                // For buildings/altars, keep icon rotation fixed relative to world
                m_iconInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }

    private void Update()
    {
        if (m_iconInstance == null) return;

        // Keep icons at a fixed height above the terrain for the minimap camera
        Vector3 pos = transform.position;
        pos.y = 80f; // Height for icons
        m_iconInstance.transform.position = pos;

        if (markerType == MarkerType.Player)
        {
            // Sync icon Y rotation to player's Y rotation, but laid flat for 2D camera
            float angle = transform.eulerAngles.y;
            m_iconInstance.transform.rotation = Quaternion.Euler(90f, angle, 0f);
        }
    }
}
