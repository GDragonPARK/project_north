using UnityEngine;

[ExecuteInEditMode]
public class GhibliWind : MonoBehaviour
{
    [Range(0, 1)] public float windIntensity = 0.5f;
    [Range(0, 360)] public float windDirection = 45f;
    public float windSpeed = 1.0f;

    void Update()
    {
        // Convert direction angle to vector
        float rad = windDirection * Mathf.Deg2Rad;
        Vector4 windVec = new Vector4(Mathf.Cos(rad), 0, Mathf.Sin(rad), windSpeed * windIntensity);
        
        Shader.SetGlobalVector("_WindDirection", windVec); // Common convention
        // Also try specific names if found later
        Shader.SetGlobalFloat("_WindStrength", windIntensity);
    }
}