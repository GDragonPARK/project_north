using UnityEngine;

/// <summary>
/// Singleton utility for building system audio & VFX feedback.
/// Attach to BuildingManager_System or any persistent GameObject.
/// Assign AudioClips and VFX prefabs in Inspector; works gracefully with null references.
/// </summary>
public class BuildingFeedback : MonoBehaviour
{
    public static BuildingFeedback Instance { get; private set; }

    [Header("Audio Clips")]
    [Tooltip("건축 성공 시 재생 (Hammer Hit)")]
    public AudioClip placeSound;
    [Tooltip("자원 부족 / 충돌 시 재생 (Error Buzz)")]
    public AudioClip errorSound;
    [Tooltip("해체 / 붕괴 시 재생 (Wood Break)")]
    public AudioClip destroySound;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Range(0f, 0.2f)] public float pitchVariation = 0.1f;

    [Header("VFX Prefabs (Optional)")]
    [Tooltip("건축 시 먼지 파티클 프리팹")]
    public GameObject placeVFXPrefab;
    [Tooltip("해체/붕괴 시 파편 파티클 프리팹")]
    public GameObject destroyVFXPrefab;

    [Header("VFX Settings")]
    public float vfxLifetime = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    // ────────────────────── Audio ──────────────────────

    public void PlayPlaceSound(Vector3 position)
    {
        PlayClipAt(placeSound, position);
    }

    public void PlayErrorSound(Vector3 position)
    {
        PlayClipAt(errorSound, position);
    }

    public void PlayDestroySound(Vector3 position)
    {
        PlayClipAt(destroySound, position);
    }

    private void PlayClipAt(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        // Create temporary AudioSource for 3D positioned sound
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource src = tempGO.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 1f; // Full 3D
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.Play();
        Destroy(tempGO, clip.length + 0.1f);
    }

    // ────────────────────── VFX ──────────────────────

    public void SpawnPlaceVFX(Vector3 position)
    {
        if (placeVFXPrefab != null)
        {
            SpawnPrefabVFX(placeVFXPrefab, position);
        }
        else
        {
            SpawnFallbackDust(position);
        }
    }

    public void SpawnDestroyVFX(Vector3 position)
    {
        if (destroyVFXPrefab != null)
        {
            SpawnPrefabVFX(destroyVFXPrefab, position);
        }
        else
        {
            SpawnFallbackDebris(position);
        }
    }

    private void SpawnPrefabVFX(GameObject prefab, Vector3 position)
    {
        GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(vfx, vfxLifetime);
    }

    // ────────────────────── Fallback VFX (Runtime) ──────────────────────

    /// <summary>
    /// White dust puff — used when placeVFXPrefab is not assigned.
    /// </summary>
    private void SpawnFallbackDust(Vector3 position)
    {
        GameObject go = new GameObject("FX_PlaceDust");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.6f;
        main.startSpeed = 1.5f;
        main.startSize = 0.3f;
        main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.6f);
        main.maxParticles = 20;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // float upward

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 15)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(1f, 1.5f)
        ));

        // Set default particle material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = new Color(1f, 1f, 1f, 0.5f);

        ps.Play();
        Destroy(go, 2f);
    }

    /// <summary>
    /// Brown wood debris — used when destroyVFXPrefab is not assigned.
    /// </summary>
    private void SpawnFallbackDebris(Vector3 position)
    {
        GameObject go = new GameObject("FX_DestroyDebris");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.8f;
        main.startSpeed = 4f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(0.55f, 0.35f, 0.15f, 1f); // Wood brown
        main.maxParticles = 30;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f; // fall down fast

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 25)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.55f, 0.35f, 0.15f), 0f),
                new GradientColorKey(new Color(0.3f, 0.2f, 0.1f), 1f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // Set default particle material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = new Color(0.55f, 0.35f, 0.15f, 1f);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;

        ps.Play();
        Destroy(go, 2f);
    }
}
