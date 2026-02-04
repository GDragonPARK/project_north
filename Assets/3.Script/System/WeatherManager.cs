using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    public enum WeatherType { Clear, Rain, Storm }
    
    [Header("Current State")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float weatherTimer;
    
    [Header("Settings")]
    public float minWeatherDuration = 60f;
    public float maxWeatherDuration = 180f;
    
    [Header("Visual References")]
    public ParticleSystem rainParticles;
    public Light sunLight;
    public Color stormLightColor = new Color(0.4f, 0.4f, 0.5f);
    
    private Color m_originalSunColor;
    private float m_originalIntensity;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (sunLight != null)
        {
            m_originalSunColor = sunLight.color;
            m_originalIntensity = sunLight.intensity;
        }
        weatherTimer = Random.Range(minWeatherDuration, maxWeatherDuration);
    }

    private void Update()
    {
        weatherTimer -= Time.deltaTime;
        if (weatherTimer <= 0)
        {
            ChangeWeather();
        }

        UpdateVisuals();
    }

    private void ChangeWeather()
    {
        // Simple random transition
        float rand = Random.value;
        if (rand < 0.6f) currentWeather = WeatherType.Clear;
        else if (rand < 0.9f) currentWeather = WeatherType.Rain;
        else currentWeather = WeatherType.Storm;

        weatherTimer = Random.Range(minWeatherDuration, maxWeatherDuration);
        Debug.Log($"<color=lightblue>Weather changed to: {currentWeather}</color>");
    }

    private void UpdateVisuals()
    {
        bool isRaining = currentWeather == WeatherType.Rain || currentWeather == WeatherType.Storm;
        
        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.enabled = isRaining;
            
            if (currentWeather == WeatherType.Storm)
                emission.rateOverTime = 500;
            else
                emission.rateOverTime = 200;
        }

        if (sunLight != null)
        {
            float targetIntensity = m_originalIntensity;
            Color targetColor = m_originalSunColor;

            if (currentWeather == WeatherType.Storm)
            {
                targetIntensity *= 0.4f;
                targetColor = stormLightColor;
            }
            else if (currentWeather == WeatherType.Rain)
            {
                targetIntensity *= 0.7f;
            }

            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, Time.deltaTime);
            sunLight.color = Color.Lerp(sunLight.color, targetColor, Time.deltaTime);
        }
    }
}
