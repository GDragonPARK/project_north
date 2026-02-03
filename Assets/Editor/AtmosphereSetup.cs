using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Antigravity.Editor
{
    public class AtmosphereSetup : EditorWindow
    {
        [MenuItem("Tools/Setup Atmosphere (Global Volume)")]
        public static void GlobalVolumeSetup()
        {
            // 1. Create or Find Global Volume Object
            GameObject volumeGo = GameObject.Find("Global Volume");
            if (volumeGo == null)
            {
                volumeGo = new GameObject("Global Volume");
                volumeGo.layer = LayerMask.NameToLayer("Default");
            }

            Volume volume = volumeGo.GetComponent<Volume>();
            if (volume == null) volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;

            // 2. Create Profile
            if (volume.profile == null)
            {
                // Create a new profile in Assets
                string path = "Assets/Settings/GlobalVolumeProfile.asset";
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                    Debug.Log("Created new Global Volume Profile at " + path);
                }
                volume.profile = profile;
            }

            // 3. Add Overrides
            var profileRef = volume.profile;

            // Fog (URP uses 'Fog' in lighting settings usually, but let's check for Volumetric Fog or Environment modifications if supported)
            // URP 12+ has limited Volumetric Fog, mostly strictly standard Fog or via additional assets. 
            // We'll set render settings for Unity Fog and try adding Color Grading.

            // RenderSettings Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f; // High density for "Dreamy" look
            RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.8f); // Bluish mist

            // Color Adjustments (Tone Mapping)
            ColorAdjustments colorAdj;
            if (!profileRef.TryGet(out colorAdj)) colorAdj = profileRef.Add<ColorAdjustments>(true);
            
            colorAdj.active = true;
            colorAdj.postExposure.value = 0.5f; // Slightly brighter
            colorAdj.contrast.value = 15f; 
            colorAdj.saturation.value = 20f; // Vibrant Ghibli style

            // Bloom (Glowing light)
            Bloom bloom;
            if (!profileRef.TryGet(out bloom)) bloom = profileRef.Add<Bloom>(true);
            
            bloom.active = true;
            bloom.intensity.value = 1.2f;
            bloom.threshold.value = 0.9f;
            bloom.scatter.value = 0.7f;

            // Vignette (Focus center)
            Vignette vignette;
            if (!profileRef.TryGet(out vignette)) vignette = profileRef.Add<Vignette>(true);
            
            vignette.active = true;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.value = 0.4f;

            // White Balance (Warmth)
            WhiteBalance wb;
            if (!profileRef.TryGet(out wb)) wb = profileRef.Add<WhiteBalance>(true);
            wb.temperature.value = -10f; // Slight cool tint for forest, or +10 for sunny. Let's go neutral-cool.

            EditorUtility.SetDirty(profileRef);
            AssetDatabase.SaveAssets();

            Debug.Log("Atmosphere Setup Complete: Fog, Bloom, Color Adjustments applied.");
        }
    }
}
