using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SnapSocketData
{
    public SnapPoint sp;
    public Vector3   localPos;
    public Quaternion localRot;
}

/// <summary>
/// [Phase 6.1-8] 진정한 발헤임식 이웃 기반 하중 네트워크 알고리즘
/// Physics.OverlapBox로 이웃 블록의 안정도 단계를 상속. 가로/세로 모두 지원.
/// </summary>
public class BuildingPiece : MonoBehaviour
{
    // ── Snap System ──
    [Header("Snap System")]
    [SerializeField] private List<SnapPoint>       snapPoints  = new List<SnapPoint>();
    [SerializeField] private List<SnapSocketData>  snapSockets = new List<SnapSocketData>();

    // ── Building Data ──
    [Header("Resource Cost")]
    public string requiredItemName = "Wood";
    public int    requiredAmount   = 2;

    [Header("Building Data")]
    public BuildingDataSO data;

    // ── Effects ──
    [Header("Effects")]
    public GameObject breakEffectPrefab; // 파괴될 때 터질 나무 파편 파티클 프리팹

    // ── Stability Settings ──
    [Header("Stability Settings")]
    public LayerMask groundLayer = 1 << 0;
    public int maxStabilitySteps = 6; // 6단계 멀어지면 파괴
    public int currentStabilityStep = 0;
    public bool isInitialized = false;

    // ── Compatibility (BuildingGhost 등 외부 참조용) ──
    /// <summary>BuildingGhost.cs에서 이웃 탐색 시 사용하는 상수</summary>
    public const float DECAY_PER_STEP = 0.2f;

    /// <summary>안정도를 0~1 로 변환 (BuildingGhost.cs 호환 프로퍼티)</summary>
    public float stability => currentStabilityStep == 0
        ? 1f
        : Mathf.Clamp01(1f - (float)currentStabilityStep / maxStabilitySteps);

    // ── Renderer Cache ──
    private MaterialPropertyBlock propBlock;
    private Renderer[]            cachedRenderers;

    // ── SnapPoint Accessors ──
    public List<SnapPoint>            SnapPoints    => snapPoints;
    public IReadOnlyList<SnapPoint>   CachedSockets => snapPoints;

    // ════════════════════════════════════ Lifecycle ════════════════════════════════════

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

    private void OnDestroy()
    {
        // 이웃 블록들의 isInitialized를 해제하여 재계산 유도
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Vector3 center  = col.bounds.center;
        Vector3 extents = col.bounds.extents + new Vector3(0.15f, 0.15f, 0.15f);
        Collider[] hits = Physics.OverlapBox(center, extents, transform.rotation);

        foreach (var h in hits)
        {
            if (h.gameObject == this.gameObject) continue;
            BuildingPiece neighbor = h.GetComponentInParent<BuildingPiece>();
            if (neighbor != null && neighbor != this)
                neighbor.isInitialized = false;
        }
    }

    // ════════════════════════════════════ Snap Caching ════════════════════════════════════

    [ContextMenu("Cache Snap Points")]
    public void CacheSnapPoints()
    {
        snapPoints.Clear();
        snapSockets.Clear();

        SnapPoint[] points = GetComponentsInChildren<SnapPoint>(true);
        foreach (var sp in points)
        {
            snapPoints.Add(sp);
            snapSockets.Add(new SnapSocketData
            {
                sp       = sp,
                localPos = transform.InverseTransformPoint(sp.transform.position),
                localRot = Quaternion.Inverse(transform.rotation) * sp.transform.rotation
            });
        }
    }

    // ════════════════════════════════════ Stability System ════════════════════════════════════

    /// <summary>
    /// 배치 직후 및 이웃 파괴 시 호출.
    /// OverlapBox로 지면 접촉 여부와 이웃 안정도 단계를 계산하고
    /// 6단계 이상이면 즉시 붕괴, 아니면 색상을 갱신한다.
    /// </summary>
    public void CalculateAndShowStability()
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Collider col = GetComponent<Collider>();

        if (col == null) return;

        Vector3 center  = col.bounds.center;
        Vector3 extents = col.bounds.extents + new Vector3(0.15f, 0.15f, 0.15f);

        // ① 내가 직접 땅(Terrain)에 닿아 있는가?
        bool touchesGround = Physics.CheckBox(center, extents, transform.rotation, groundLayer);

        if (touchesGround)
        {
            currentStabilityStep = 0; // 기초 (파랑)
        }
        else
        {
            // ② 이웃 블록 중 가장 튼튼한(숫자가 낮은) 놈을 찾음
            int bestNeighborStep = 999;
            Collider[] hits = Physics.OverlapBox(center, extents, transform.rotation);

            foreach (var h in hits)
            {
                if (h.gameObject == this.gameObject) continue;
                BuildingPiece neighbor = h.GetComponentInParent<BuildingPiece>();
                if (neighbor != null && neighbor.isInitialized)
                {
                    if (neighbor.currentStabilityStep < bestNeighborStep)
                        bestNeighborStep = neighbor.currentStabilityStep;
                }
            }

            // 이웃보다 1단계 더 약해짐 (가로/세로 무관하게 전염)
            currentStabilityStep = bestNeighborStep + 1;
        }

        isInitialized = true;

        // ③ 기획: 6단계 이상 멀어지면 하중을 견디지 못하고 붕괴!
        if (currentStabilityStep >= maxStabilitySteps)
        {
            Debug.Log($"<color=red>[Stability]</color> {gameObject.name} 하중 지지 불가! -> 코루틴 붕괴 시작");
            StartCoroutine(CollapseRoutine()); // 0.5초 후 VFX + Destroy 동기화
            // return 없음 → 아래 빨간색 렌더링 로직이 즉시 실행됨
        }

        // ④ 단계에 따른 색상 결정 후 발광 렌더링
        Color stabilityColor = GetColorFromStep(currentStabilityStep);

        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color",         stabilityColor);
                propBlock.SetColor("_BaseColor",     stabilityColor);
                propBlock.SetColor("_EmissionColor", stabilityColor * 2.0f);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private Color GetColorFromStep(int step)
    {
        if (step == 0) return Color.blue;

        // 1단계~5단계를 0.0 ~ 1.0 비율로 변환
        float t = (float)step / (maxStabilitySteps - 1);

        if (t <= 0.25f) return Color.Lerp(Color.green, Color.yellow, t / 0.25f);
        if (t <= 0.5f)  return Color.yellow;
        if (t <= 0.75f) return Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (t - 0.5f) / 0.25f);
        return Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (t - 0.75f) / 0.25f);
    }

    /// <summary>BuildingGhost.cs 호환: stability 수치(0~1)로 색상 반환</summary>
    public static Color GetStabilityColor(float s)
    {
        if (s >= 1.0f) return Color.blue;
        if (s >= 0.7f) return Color.Lerp(Color.yellow, Color.green, (s - 0.7f) / 0.3f);
        if (s >= 0.4f) return Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, (s - 0.4f) / 0.3f);
        return Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), s / 0.4f);
    }

    // ════════════════════════════════════ Collapse Coroutine ════════════════════════════════════

    private System.Collections.IEnumerator CollapseRoutine()
    {
        // 1. 빨간색으로 변한 상태에서 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // 2. 파괴 이펙트 펑! (파티클 생성)
        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        // 3. 자원 환불
        RefundResources();

        // 4. 오브젝트 완전 파괴
        Destroy(gameObject);
    }

    // ════════════════════════════════════ Resource Refund ════════════════════════════════════

    public void RefundResources()
    {
        if (data == null || data.constructionCosts == null) return;

        foreach (var cost in data.constructionCosts)
        {
            if (cost.item != null && BuildingManager.Instance != null)
                BuildingManager.Instance.AddResource(cost.item, cost.amount);
        }
    }
}
