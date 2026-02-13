using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VegetationOptimizer : EditorWindow
{
    [MenuItem("Antigravity/🚀 Optimize Vegetation")]
    public static void OptimizePerformance()
    {
        Debug.Log("[Optimizer] 🚀 Starting Performance Optimization...");

        // 1. Enable GPU Instancing on Vegetation Materials
        VegetationSpawner vs = FindObjectOfType<VegetationSpawner>();
        if (vs != null)
        {
            Renderer[] renderers = vs.GetComponentsInChildren<Renderer>(true);
            HashSet<Material> processedMats = new HashSet<Material>();
            int matCount = 0;

            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && !processedMats.Contains(m))
                    {
                        if (!m.enableInstancing)
                        {
                            m.enableInstancing = true;
                            matCount++;
                        }
                        processedMats.Add(m);
                    }
                }
            }
            Debug.Log($"[Optimizer] ✅ Enabled GPU Instancing on {matCount} unique materials found in Vegetation.");
        }
        else
        {
            Debug.LogWarning("[Optimizer] ⚠️ VegetationSpawner not found! Skipping material optimization.");
        }

        // 2. Adjust Quality Settings
        // Shadow Distance
        float oldShadow = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance = 70f;
        Debug.Log($"[Optimizer] 📉 Shadow Distance: {oldShadow} -> 70.0");

        // LOD Bias
        float oldLOD = QualitySettings.lodBias;
        QualitySettings.lodBias = 0.7f;
        Debug.Log($"[Optimizer] 📉 LOD Bias: {oldLOD} -> 0.7");

        Debug.Log("[Optimizer] ✨ Optimization Complete!");
    }
}
