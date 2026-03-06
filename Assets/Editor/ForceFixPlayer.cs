using UnityEngine;
using UnityEditor;

public class ForceFixPlayer : MonoBehaviour
{
    [MenuItem("Antigravity/Force Fix Player Sync (Phase 11.10)")]
    public static void ExecuteFix()
    {
        Debug.Log("<color=orange><b>[Phase 11.10 - Player Sync Fix]</b></color> Starting verification...");
        
        GameObject player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("Player_New object not found in the scene! Cannot proceed with fix.");
            return;
        }

        // 1. CharacterStats 부착 검증 및 강제 주입
        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogWarning("CharacterStats is missing from Player_New! Attaching immediately.");
            stats = player.AddComponent<CharacterStats>();
        }

        // 2. 프리팹 혹은 씬 상태 더티 마킹 (저장 유도)
        EditorUtility.SetDirty(player);
        if (stats != null) EditorUtility.SetDirty(stats);

        // 3. (옵션) 중복 CharacterStats가 다른 곳에 있는지 스캔
        CharacterStats[] allStats = Object.FindObjectsByType<CharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in allStats)
        {
            if (s.gameObject != player)
            {
                Debug.LogWarning($"<color=red><b>[Warning]</b></color> Found duplicate CharacterStats on {s.gameObject.name}. This may cause Singleton conflicts. Destroying component...");
                DestroyImmediate(s);
            }
        }

        Debug.Log("<color=cyan><b>[Player Sync Fix Complete]</b></color> CharacterStats uniqueness guaranteed and attached to Player_New.");
    }
}
