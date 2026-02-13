using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VegetationForceSpawn : EditorWindow
{
    [MenuItem("Antigravity/🌲 FORCE Spawn & Rescue")]
    public static void ForceSpawnAndRescue()
    {
        Debug.Log("[ForceSpawn] 🎬 ACTION: Starting Vegetation Restoration Protocol...");

        // 1. Find Components
        TerrainGenerator tg = FindObjectOfType<TerrainGenerator>();
        VegetationSpawner vs = FindObjectOfType<VegetationSpawner>();

        if (tg == null) { Debug.LogError("❌ TerrainGenerator not found!"); return; }
        if (vs == null) { Debug.LogError("❌ VegetationSpawner not found!"); return; }

        Undo.RecordObject(vs, "Sync Vegetation Lists");

        // 2. Sync Lists (TG -> VS)
        // Ensure VS has lists initialized
        if (vs.treePrefabs == null) vs.treePrefabs = new List<GameObject>();
        if (vs.flowerPrefabs == null) vs.flowerPrefabs = new List<GameObject>();
        if (vs.shrubPrefabs == null) vs.shrubPrefabs = new List<GameObject>();

        // Copy from TG if TG has data
        if (tg.treePrefabs != null && tg.treePrefabs.Count > 0)
        {
            vs.treePrefabs = new List<GameObject>(tg.treePrefabs);
            Debug.Log($"[ForceSpawn] 📥 Synced {vs.treePrefabs.Count} Trees from Generator.");
        }
        
        if (tg.flowerPrefabs != null && tg.flowerPrefabs.Count > 0)
        {
            vs.flowerPrefabs = new List<GameObject>(tg.flowerPrefabs);
            Debug.Log($"[ForceSpawn] 📥 Synced {vs.flowerPrefabs.Count} Flowers from Generator.");
        }

        if (tg.shrubPrefabs != null && tg.shrubPrefabs.Count > 0)
        {
            vs.shrubPrefabs = new List<GameObject>(tg.shrubPrefabs);
            Debug.Log($"[ForceSpawn] 📥 Synced {vs.shrubPrefabs.Count} Shrubs from Generator.");
        }

        // 3. Force Spawn
        Debug.Log($"[ForceSpawn] 🌱 Spawning World... (Density: {tg.objectDensity})");
        vs.SpawnFullWorld(tg.width, tg.length, tg.objectDensity);
        
        // 4. Player Rescue
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            Undo.RecordObject(player.transform, "Teleport Player");
            
            // Safety: Reset Velocity if Rigidbody exists
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Teleport to Sky (Y=50) to settle physics
            Vector3 currentPos = player.transform.position;
            Vector3 safePos = new Vector3(currentPos.x, 50f, currentPos.z);
            player.transform.position = safePos;
            
            Debug.Log($"[ForceSpawn] 🦅 Operation Sky Drop: Player teleported to {safePos}");
        }
        else
        {
            Debug.LogWarning("[ForceSpawn] ⚠️ Player not found! Skipped rescue.");
        }

        Debug.Log("[ForceSpawn] ✅ MISSION COMPLETE.");
    }
}
