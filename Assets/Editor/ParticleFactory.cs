using UnityEngine;
using UnityEditor;

public class ParticleFactory : EditorWindow
{
    [MenuItem("Tools/Create Wood Particle")]
    public static void Create()
    {
        GameObject go = new GameObject("VFX_WoodHit");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1.0f;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(0.6f, 0.4f, 0.2f); // Wood brown
        main.gravityModifier = 1f;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = false; // We will Emit manually or use Burst
        // Burst
        emission.SetBursts(new ParticleSystem.Burst[]{ new ParticleSystem.Burst(0.0f, 5, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit")); 
        
        // Save as Prefab
        string path = "Assets/VFX_WoodHit.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        
        Debug.Log($"Created Particle Prefab at {path}");
    }
}