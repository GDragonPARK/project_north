using UnityEngine;
using UnityEditor;

public class WoodcuttingSetupV2 : EditorWindow
{
    [MenuItem("Tools/Setup Woodcutting V2")]
    public static void Setup()
    {
        // 1. Ensure Particle Exists (Legacy check, now handled by ObjectPoolManager in scene)
        // ParticleFactory.Create(); 
        
        // 2. Find Logs
        string logPath = "Assets/valheim_Data/GameElements/Items/materials/RoundLog.prefab";
        GameObject log = AssetDatabase.LoadAssetAtPath<GameObject>(logPath);
        
        if(!log) Debug.LogError("Log Prefab not found!");
        
        // 3. Update Scene Trees
        TreeFelling[] trees = FindObjectsOfType<TreeFelling>();
        int count = 0;
        foreach(var t in trees)
        {
            // t.hitParticlePrefab = vfx; // REMOVED - Uses ObjectPool tag "WoodChip"
            t.logPrefab = log;
            
            // Health is now in HealthSystem
            HealthSystem hp = t.GetComponent<HealthSystem>();
            if(hp == null) hp = t.gameObject.AddComponent<HealthSystem>();
            
            hp.maxHealth = 50f;
            count++;
        }
        Debug.Log($"Updated {count} Trees with Log prefabs and HealthSystem.");
        
        // 4. Fix Camera
        CameraCollisionSetup.Fix();
    }
}