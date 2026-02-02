using UnityEngine;
using UnityEditor;

public class VerifySpawn
{
    [MenuItem("Tools/Verify Spawn Fix")]
    public static void Verify()
    {
        GameObject p = GameObject.Find("Player_New");
        GameObject s = GameObject.Find("Spawn_Point");
        
        if (p) Debug.Log($"Player found at {p.transform.position}");
        else Debug.LogError("Player_New NOT FOUND");

        if (s) Debug.Log($"Spawn Point found at {s.transform.position}");
        else Debug.LogError("Spawn_Point NOT FOUND");

        if (p && s)
        {
            float dist = Vector3.Distance(p.transform.position, s.transform.position);
            if (dist < 0.1f) Debug.Log("SUCCESS: Player is at Spawn Point.");
            else Debug.LogWarning($"MISMATCH: Player is {dist}m away from Spawn.");
        }
    }
}