using UnityEngine;
using UnityEditor;

public class WoodcuttingSetupV2 : EditorWindow
{
    [MenuItem("Tools/Setup Woodcutting V2")]
    public static void Setup()
    {
        // 1. Ensure Particle Exists
        ParticleFactory.Create(); 
        string particlePath = "Assets/VFX_WoodHit.prefab";
        GameObject vfx = AssetDatabase.LoadAssetAtPath<GameObject>(particlePath);
        
        // 2. Find Logs
        string logPath = "Assets/valheim_Data/GameElements/Items/materials/RoundLog.prefab";
        GameObject log = AssetDatabase.LoadAssetAtPath<GameObject>(logPath);
        
        if(!vfx) Debug.LogError("VFX Prefab not found!");
        if(!log) Debug.LogError("Log Prefab not found!");
        
        // 3. Update Scene Trees
        TreeFelling[] trees = FindObjectsOfType<TreeFelling>();
        int count = 0;
        foreach(var t in trees)
        {
            t.hitParticlePrefab = vfx;
            t.logPrefab = log;
            t.maxHealth = 50f; // Ensure killable (e.g. 2-3 hits if dmg is 20)
            count++;
        }
        Debug.Log($"Updated {count} Trees with VFX and Log prefabs.");
        
        // 4. Fix Camera
        CameraCollisionSetup.Fix();
    }
}