using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    [Range(0, 24)] public float currentTime = 8f; // 8:00 AM
    public float dayLengthSeconds = 1200f; // 20 minutes for a full day
    
    [Header("References")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    public Gradient fogColor;

    [Header("Calculated States")]
    public bool isNight;
    private float m_timeMultiplier;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        m_timeMultiplier = 24f / dayLengthSeconds;
    }

    private void Update()
    {
        UpdateTime();
        UpdateLighting();
    }

    private void UpdateTime()
    {
        currentTime += Time.deltaTime * m_timeMultiplier;
        if (currentTime >= 24f) currentTime = 0;

        isNight = (currentTime < 6f || currentTime > 18f); // Night from 6 PM to 6 AM
    }

    private void UpdateLighting()
    {
        if (sunLight == null) return;

        // Rotate Sun
        float sunRotation = (currentTime / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunRotation, -170f, 0);

        // Intensity and Color
        float timePercent = currentTime / 24f;
        sunLight.intensity = sunIntensity.Evaluate(timePercent);
        sunLight.color = sunColor.Evaluate(timePercent);

        // Fog
        RenderSettings.fogColor = fogColor.Evaluate(timePercent);
        RenderSettings.fogDensity = isNight ? 0.05f : 0.02f;
    }

    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        return $"{hours:00}:{minutes:00}";
    }
}
