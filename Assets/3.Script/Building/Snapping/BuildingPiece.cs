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
    [SerializeField] private List<SnapSocketData> snapSockets = new List<SnapSocketData>();

    [Header("Stability & Data")]
    public BuildingDataSO data;
    public bool isGrounded = false;

    [Header("Stability")]
    [Range(0f, 1f)] public float stability = 0f;

    // ── Constants ──
    public  const float DECAY_PER_STEP        = 0.2f;
    public  const float COLLAPSE_THRESHOLD     = 0.1f;
    private const float BASE_COLLAPSE_DELAY    = 2.5f;
    private const float SEARCH_RADIUS          = 1.8f;
    private const float EMISSION_INTENSITY     = 0.3f;

    // ── Collapse state ──
    private bool  isCollapsing  = false;
    private float collapseTimer = 0f;

    // ── Renderer cache ──
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;

    public List<SnapPoint> SnapPoints => snapPoints;

    // ────────────────────── Lifecycle ──────────────────────

    private void Awake()
    {
        CacheSnapPoints();
        cachedRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        CacheSnapPoints();
    }

    private void Update()
    {
        // Collapse countdown
        if (stability < COLLAPSE_THRESHOLD && !isGrounded)
        {
            if (!isCollapsing)
            {
                isCollapsing = true;
                float weightMult = (data != null) ? data.weight : 1.0f;
                collapseTimer = BASE_COLLAPSE_DELAY * weightMult;
                Debug.Log($"[BuildingPiece] {gameObject.name} 붕괴 시작! ({collapseTimer:F1}초 후 파괴, weight={weightMult})");
            }

            collapseTimer -= Time.deltaTime;
            if (collapseTimer <= 0f)
            {
                Collapse();
            }
        }
        else if (isCollapsing)
        {
            // Stability recovered (e.g., new support was added)
            isCollapsing = false;
            collapseTimer = 0f;
        }
    }

    private void OnDestroy()
    {
        // Notify neighbors to recalculate when this piece is destroyed
        List<BuildingPiece> neighbors = GetNeighborPieces();
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null && neighbor.gameObject != null)
            {
                // Delay one frame so Destroy() finishes first
                neighbor.StartCoroutine(neighbor.DelayedPropagation());
            }
        }
    }

    private System.Collections.IEnumerator DelayedPropagation()
    {
        yield return null; // wait one frame
        PropagateStabilityUpdate(new HashSet<int>());
    }

    // ────────────────────── Snap Caching ──────────────────────

    [ContextMenu("Cache Snap Points")]
    public void CacheSnapPoints()
    {
        snapPoints.Clear();
        snapSockets.Clear();

        SnapPoint[] points = GetComponentsInChildren<SnapPoint>(true);

        foreach (var sp in points)
        {
            snapPoints.Add(sp);

            SnapSocketData d = new SnapSocketData
            {
                sp = sp,
                localPos = transform.InverseTransformPoint(sp.transform.position),
                localRot = Quaternion.Inverse(transform.rotation) * sp.transform.rotation
            };
            snapSockets.Add(d);
        }
    }

    // ────────────────────── Ground Check ──────────────────────

    public void CheckGroundedStatus()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            Collider[] hits = Physics.OverlapSphere(
                col.bounds.center,
                col.bounds.extents.magnitude,
                LayerMask.GetMask("Terrain", "Stone", "Foundation"));

            if (hits.Length > 0)
            {
                isGrounded = true;
                return;
            }
        }

        isGrounded = false;
    }

    // ────────────────────── Stability Engine ──────────────────────

    /// <summary>
    /// Recalculate this piece's stability based on grounded status or neighbors.
    /// </summary>
    public void UpdateStability()
    {
        if (isGrounded)
        {
            stability = 1.0f;
            ApplyStabilityColor();
            return;
        }

        // Find the best supporting neighbor
        List<BuildingPiece> neighbors = GetNeighborPieces();
        float bestNeighborStability = 0f;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.stability > bestNeighborStability)
            {
                bestNeighborStability = neighbor.stability;
            }
        }

        float newStability = Mathf.Max(0f, bestNeighborStability - DECAY_PER_STEP);
        stability = newStability;
        ApplyStabilityColor();
    }

    /// <summary>
    /// BFS propagation: update self, then propagate to neighbors that actually changed.
    /// </summary>
    public void PropagateStabilityUpdate(HashSet<int> visited)
    {
        int id = gameObject.GetInstanceID();
        if (visited.Contains(id)) return;
        visited.Add(id);

        float oldStability = stability;
        UpdateStability();

        // Only propagate further if our value actually changed
        if (Mathf.Abs(stability - oldStability) < 0.001f) return;

        List<BuildingPiece> neighbors = GetNeighborPieces();
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
            {
                neighbor.PropagateStabilityUpdate(visited);
            }
        }
    }

    /// <summary>
    /// Find adjacent BuildingPieces using Physics.OverlapSphere.
    /// </summary>
    private List<BuildingPiece> GetNeighborPieces()
    {
        List<BuildingPiece> result = new List<BuildingPiece>();
        Collider[] hits = Physics.OverlapSphere(transform.position, SEARCH_RADIUS);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            BuildingPiece piece = hit.GetComponent<BuildingPiece>();
            if (piece == null) piece = hit.GetComponentInParent<BuildingPiece>();

            if (piece != null && piece != this && !result.Contains(piece))
            {
                result.Add(piece);
            }
        }

        return result;
    }

    // ────────────────────── Visual Feedback ──────────────────────

    /// <summary>
    /// Apply color tint to all renderers based on stability value.
    /// 1.0 Cyan → 0.8 Green → 0.6 Yellow → 0.4 Orange → 0.2 Red
    /// </summary>
    public void ApplyStabilityColor()
    {
        if (cachedRenderers == null) return;

        Color tint = GetStabilityColor(stability);
        Color emission = tint * EMISSION_INTENSITY;

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);

            // URP uses _BaseColor, Standard uses _Color
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                propBlock.SetColor("_BaseColor", tint);
            else
                propBlock.SetColor("_Color", tint);

            // Emission for night visibility
            propBlock.SetColor("_EmissionColor", emission);

            r.SetPropertyBlock(propBlock);

            // Enable emission keyword on shared material (one-time cost)
            if (r.sharedMaterial != null && !r.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            {
                r.sharedMaterial.EnableKeyword("_EMISSION");
                r.sharedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
    }

    /// <summary>
    /// Returns a color corresponding to the given stability value.
    /// </summary>
    public static Color GetStabilityColor(float s)
    {
        // 5-stop gradient: Red(0) → Orange(0.2) → Yellow(0.4) → Green(0.6) → Cyan(0.8+)
        if (s >= 0.8f) return Color.Lerp(Color.green, Color.cyan, (s - 0.8f) / 0.2f);
        if (s >= 0.6f) return Color.Lerp(Color.yellow, Color.green, (s - 0.6f) / 0.2f);
        if (s >= 0.4f) return Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, (s - 0.4f) / 0.2f);
        if (s >= 0.2f) return Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), (s - 0.2f) / 0.2f);
        return Color.red;
    }

    // ────────────────────── Collapse ──────────────────────

    private void Collapse()
    {
        Debug.Log($"[BuildingPiece] {gameObject.name} 붕괴!");

        // Audio & VFX feedback
        BuildingFeedback.Instance?.PlayDestroySound(transform.position);
        BuildingFeedback.Instance?.SpawnDestroyVFX(transform.position);

        RefundResources();
        Destroy(gameObject);
    }

    // ────────────────────── Resource Refund ──────────────────────

    public void RefundResources()
    {
        if (data == null || data.constructionCosts == null) return;

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && BuildingManager.Instance != null)
            {
                BuildingManager.Instance.AddResource(cost.item, cost.amount);
            }
        }
    }
}
