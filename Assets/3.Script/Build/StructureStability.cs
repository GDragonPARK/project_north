using UnityEngine;
using System.Collections.Generic;

public class StructureStability : MonoBehaviour
{
    [Header("Stability Settings")]
    public float maxStability = 1.0f;
    public float stabilityLossPerStep = 0.12f; // Distance from ground penalty
    public LayerMask supportMask; // Layers that provide support (Terrain, Building)

    private float m_currentStability = 0f;
    private Renderer m_renderer;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor"); // URP property name

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        UpdateStability();
    }

    /// <summary>
    /// Calculates stability based on ground contact or neighbor support
    /// </summary>
    public void UpdateStability()
    {
        float previousStability = m_currentStability;

        if (CheckGroundContact())
        {
            m_currentStability = maxStability; // Foundation piece
        }
        else
        {
            float bestNeighborStability = 0f;
            // Scan for nearby building pieces that can provide support
            Collider[] neighbors = Physics.OverlapBox(transform.position, transform.localScale * 0.55f, transform.rotation, supportMask);
            
            foreach (var col in neighbors)
            {
                if (col.gameObject == gameObject) continue;
                
                StructureStability other = col.GetComponentInParent<StructureStability>();
                if (other != null && other != this)
                {
                    if (other.m_currentStability > bestNeighborStability)
                    {
                        bestNeighborStability = other.m_currentStability;
                    }
                }
            }
            
            m_currentStability = Mathf.Max(0, bestNeighborStability - stabilityLossPerStep);
        }

        VisualizeStability();

        // If stability changed, tell neighbors to re-calculate (Chain reaction)
        if (Mathf.Abs(previousStability - m_currentStability) > 0.05f)
        {
            NotifyNeighbors();
        }

        // Collapse logic: if totally unsupported
        if (m_currentStability <= 0.05f)
        {
            Invoke(nameof(Collapse), 0.5f); // Slight delay for dramatic effect
        }
    }

    private bool CheckGroundContact()
    {
        // Raycast down to find Terrain or Static environment
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, transform.localScale.y * 0.8f, LayerMask.GetMask("Terrain", "Default"));
    }

    private void VisualizeStability()
    {
        if (m_renderer == null) return;

        // Transition: Blue (Stable/Ground) -> Green -> Yellow -> Red (Unstable)
        Color color;
        if (m_currentStability >= 0.95f) color = Color.blue;
        else if (m_currentStability > 0.7f) color = Color.green;
        else if (m_currentStability > 0.4f) color = Color.yellow;
        else color = Color.red;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor(ColorProp, color);
        m_renderer.SetPropertyBlock(mpb);
    }

    private void NotifyNeighbors()
    {
        Collider[] neighbors = Physics.OverlapSphere(transform.position, 3f, supportMask);
        foreach (var col in neighbors)
        {
            if (col.gameObject == gameObject) continue;
            col.GetComponentInParent<StructureStability>()?.UpdateStability();
        }
    }

    private void Collapse()
    {
        if (m_currentStability > 0.05f) return; // Re-check in case support was added

        Debug.Log($"<color=red>Stability Failed: {gameObject.name} Collapsed!</color>");
        // TODO: Spawn destruction FX or debris
        Destroy(gameObject);
    }
}
