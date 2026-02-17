using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SnapSocketData
{
    public SnapPoint sp;
    public Vector3 localPos;
    public Quaternion localRot;
}

public class BuildingPiece : MonoBehaviour
{
    [Header("Snap System")]
    [SerializeField] private List<SnapPoint> snapPoints = new List<SnapPoint>();
    [SerializeField] private List<SnapSocketData> snapSockets = new List<SnapSocketData>(); // Cached local data

    [Header("Stability & Data")]
    public BuildingDataSO data; // Reference to building type for resource refund
    public bool isGrounded = false; // True if building touches terrain/foundation

    public List<SnapPoint> SnapPoints => snapPoints;

    private void Awake()
    {
        CacheSnapPoints();
    }

    private void OnEnable()
    {
        CacheSnapPoints(); // Ensure we have latest if enabled/disabled
    }

    [ContextMenu("Cache Snap Points")]
    public void CacheSnapPoints()
    {
        snapPoints.Clear();
        snapSockets.Clear();

        // Find all SnapPoints in children (include inactive if needed, usually active)
        SnapPoint[] points = GetComponentsInChildren<SnapPoint>(true);
        
        foreach (var sp in points)
        {
            snapPoints.Add(sp);
            
            SnapSocketData data = new SnapSocketData
            {
                sp = sp,
                localPos = transform.InverseTransformPoint(sp.transform.position),
                localRot = Quaternion.Inverse(transform.rotation) * sp.transform.rotation
            };
            snapSockets.Add(data);
        }
        
        Debug.Log($"[BuildingPiece] Cached {snapPoints.Count} SnapPoints for {gameObject.name}");
    }

    /// <summary>
    /// Check if this building is touching terrain (foundation check)
    /// </summary>
    public void CheckGroundedStatus()
    {
        // Get all colliders on this building
        Collider[] colliders = GetComponentsInChildren<Collider>();
        
        foreach (Collider col in colliders)
        {
            // Use OverlapBox/Sphere at collider position to check for terrain
            Collider[] hits = Physics.OverlapSphere(col.bounds.center, col.bounds.extents.magnitude, LayerMask.GetMask("Terrain"));
            
            if (hits.Length > 0)
            {
                isGrounded = true;
                Debug.Log($"[BuildingPiece] {gameObject.name} is grounded on terrain.");
                return;
            }
        }
        
        Debug.Log($"[BuildingPiece] {gameObject.name} is NOT grounded.");
    }

    /// <summary>
    /// Get resource refund amount (100% of construction cost)
    /// </summary>
    public void RefundResources()
    {
        if (data == null || data.constructionCosts == null) return;

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && BuildingManager.Instance != null)
            {
                BuildingManager.Instance.AddResource(cost.item.itemName, cost.amount);
            }
        }
    }
}
