using UnityEngine;

public class BuildPreviewController : MonoBehaviour
{
    [Header("Settings")]
    public float snapSearchRadius = 1.0f;
    public LayerMask snapPointLayer;
    public LayerMask buildableLayer; 
    public LayerMask obstacleLayer;
    
    [Header("Visuals")]
    [SerializeField] private Color validColor = new Color(0, 0, 1, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);
    
    private MaterialPropertyBlock m_mpb;
    private Renderer[] m_renderers;
    private BuildingPiece m_previewPiece;
    private BuildVolume m_buildVolume;
    
    private bool m_isValid = true;
    
    // PUBLIC ACCESSORS FOR MANAGER
    public bool IsValidPlacement => m_isValid;
    public Pose CurrentPose { get; private set; }
    public bool HasSnapMatch => MatchedWorldPoint != null;
    public SnapPoint MatchedWorldPoint { get; private set; }
    public string MatchedPreviewSocketId { get; private set; }

    private static Collider[] s_overlapCache = new Collider[10];

    private void Awake()
    {
        m_mpb = new MaterialPropertyBlock();
        m_renderers = GetComponentsInChildren<Renderer>(true); 
        m_previewPiece = GetComponent<BuildingPiece>();
        m_buildVolume = GetComponent<BuildVolume>();
        
        // Disable colliders
        foreach (var c in GetComponentsInChildren<Collider>())
        {
             c.enabled = false;
        }
    }

    public void UpdatePreview(Ray ray, float maxDistance, float rotationY)
    {
        Vector3 desiredPos = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, buildableLayer))
        {
            desiredPos = hit.point;
        }
        else
        {
            desiredPos = ray.GetPoint(maxDistance);
        }

        Quaternion desiredRot = Quaternion.Euler(0, rotationY, 0);
        
        CurrentPose = new Pose(desiredPos, desiredRot);
        MatchedWorldPoint = null;
        MatchedPreviewSocketId = null;

        if (SnapSolver.TrySolveSnap(desiredPos, desiredRot, m_previewPiece, snapSearchRadius, snapPointLayer, 
            out Pose solvedPose, out SnapPoint worldSp, out SnapPoint previewSp))
        {
            CurrentPose = solvedPose;
            MatchedWorldPoint = worldSp;
            if (previewSp != null) MatchedPreviewSocketId = previewSp.socketId;
        }

        transform.position = CurrentPose.position;
        transform.rotation = CurrentPose.rotation;

        CheckValidity();
        UpdateMaterial();
    }

    private void CheckValidity()
    {
        m_isValid = true;
        
        if (m_buildVolume != null)
        {
             Vector3 worldCenter = transform.TransformPoint(m_buildVolume.center);
             Vector3 halfSize = m_buildVolume.size * 0.5f; 
             Vector3 scaledHalfSize = Vector3.Scale(halfSize, transform.lossyScale);

             int hitCount = Physics.OverlapBoxNonAlloc(
                 worldCenter, 
                 scaledHalfSize, 
                 s_overlapCache, 
                 transform.rotation, 
                 obstacleLayer, 
                 QueryTriggerInteraction.Collide);
             
             if (hitCount > 0)
             {
                 m_isValid = false;
             }
        }
    }

    private void UpdateMaterial()
    {
        Color targetColor = m_isValid ? validColor : invalidColor;
        
        m_mpb.SetColor("_BaseColor", targetColor);
        m_mpb.SetColor("_Color", targetColor); 
        
        for (int i = 0; i < m_renderers.Length; i++)
        {
            if (m_renderers[i] != null)
                m_renderers[i].SetPropertyBlock(m_mpb);
        }
    }
}
