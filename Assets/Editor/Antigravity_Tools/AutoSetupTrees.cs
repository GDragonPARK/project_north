using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [Phase 7.1-2] 프로젝트 내 모든 나무 프리팹을 스캔하여
/// HealthSystem + TreeFelling 컴포넌트 자동 부착 및 VFX 일괄 할당.
/// </summary>
public static class AutoSetupTrees
{
    private const string VFX_PATH = "Assets/VFX_WoodHit.prefab";

    // 나무로 판단할 이름 키워드 (소문자 비교)
    private static readonly string[] TREE_KEYWORDS = new[]
    {
        "tree", "pine", "fir", "oak", "birch", "spruce", "cedar", "beech", "willow"
    };

    // 나무가 아닌 것으로 판단할 제외 키워드
    private static readonly string[] EXCLUDE_KEYWORDS = new[]
    {
        "vfx", "fx", "log", "stub", "sapling", "room", "fire", "smoke", "bonfire", "camp", "ashlands"
    };

    [MenuItem("Tools/Valheim/Setup All Trees")]
    public static void SetupAllTrees()
    {
        // 1. VFX 프리팹 로드
        var vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VFX_PATH);
        if (vfxPrefab == null)
            Debug.LogWarning($"[AutoSetupTrees] VFX 프리팹 없음: {VFX_PATH} (VFX 할당 없이 계속 진행)");

        // 2. [Phase 8.1] SFX 클립 탐색
        AudioClip hitSound  = FindFirstAudioClip("Assets/valheim_Data/Audio/Audio/sfx/hit/tree_hit");
        AudioClip fallSound = FindFirstAudioClip("Assets/valheim_Data/Audio/Audio/sfx/objects/trees", "Fall_0");

        if (hitSound  != null) Debug.Log($"[AutoSetupTrees] hitSound  : {hitSound.name}");
        if (fallSound != null) Debug.Log($"[AutoSetupTrees] fallSound : {fallSound.name}");

        // 3. 프로젝트 내 모든 Prefab GUID 수집
        string[] allGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        var treePrefabPaths = new List<string>();
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string nameLower = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            foreach (string keyword in TREE_KEYWORDS)
            {
                if (nameLower.Contains(keyword)) { treePrefabPaths.Add(path); break; }
            }
        }

        if (treePrefabPaths.Count == 0)
        {
            Debug.LogWarning("[AutoSetupTrees] 나무 키워드에 해당하는 프리팹을 찾지 못했습니다.");
            EditorUtility.DisplayDialog("AutoSetupTrees", "나무 프리팹을 찾지 못했습니다.\n프리팹 이름에 tree/pine/oak 등이 포함되어야 합니다.", "확인");
            return;
        }

        int successCount = 0;
        int skipCount    = 0;

        foreach (string path in treePrefabPaths)
        {
            if (path.Contains("/Editor/")) { skipCount++; continue; }

            string nameLowerCheck = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            bool isExcluded = false;
            foreach (string ex in EXCLUDE_KEYWORDS)
            {
                if (nameLowerCheck.Contains(ex)) { isExcluded = true; break; }
            }
            if (isExcluded) { skipCount++; continue; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;

                // A. HealthSystem
                var hs = root.GetComponent<HealthSystem>();
                if (hs == null) hs = root.AddComponent<HealthSystem>();
                if (hs.maxHealth <= 0f) hs.maxHealth = 50f;

                // B. TreeFelling
                var tf = root.GetComponent<TreeFelling>();
                if (tf == null) tf = root.AddComponent<TreeFelling>();

                // C. VFX
                if (vfxPrefab != null) tf.hitEffectPrefab = vfxPrefab;

                // D. [Phase 8.1] SFX 할당
                if (hitSound  != null && tf.hitSound  == null) tf.hitSound  = hitSound;
                if (fallSound != null && tf.fallSound == null) tf.fallSound = fallSound;

                // E. 태그 자동 설정
                if (root.tag == "Untagged" || string.IsNullOrEmpty(root.tag))
                {
                    var tagList = new System.Collections.Generic.List<string>(UnityEditorInternal.InternalEditorUtility.tags);
                    if (tagList.Contains("Tree")) root.tag = "Tree";
                }

                RemoveMissingScriptsRecursively(root);
                Debug.Log($"[AutoSetupTrees] ✅ 설정 완료: {root.name}");
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"총 {successCount}개 나무 프리팹에 벌목+SFX 세팅 완료\n(건너뜀: {skipCount}개)";
        Debug.Log($"[AutoSetupTrees] {msg}");
        EditorUtility.DisplayDialog("AutoSetupTrees 완료", msg, "확인");
    }

/// <summary>
    /// [Phase 7.2] 모든 나무 프리팹의 ResourceNode.lootPrefab 필드에
    /// Wood 아이템 프리팹(Assets/Prefabs/Wood.prefab)을 자동 연결한다.
    /// </summary>
    [MenuItem("Tools/Valheim/Phase 7.2 Connect lootPrefabs")]
    public static void SetupLootPrefabs()
    {
        const string LOOT_PREFAB_PATH = "Assets/Prefabs/Wood.prefab";
        var woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LOOT_PREFAB_PATH);
        if (woodPrefab == null)
        {
            Debug.LogError($"[Phase 7.2] Wood 프리팹 없음: {LOOT_PREFAB_PATH}");
            EditorUtility.DisplayDialog("Error", $"Wood prefab not found:\n{LOOT_PREFAB_PATH}", "OK");
            return;
        }

        string[] allGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int successCount = 0;
        int skipCount = 0;

        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Editor/") || path.Contains("/valheim_Data/")) { skipCount++; continue; }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) { skipCount++; continue; }
            if (prefabAsset.GetComponent<ResourceNode>() == null) { skipCount++; continue; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var resNode = root.GetComponent<ResourceNode>();
                if (resNode == null) { skipCount++; continue; }

                resNode.lootPrefab = woodPrefab;
                Debug.Log($"[Phase 7.2] lootPrefab 연결 완료: {root.name}");
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"ResourceNode lootPrefab 연결: {successCount}개 완료\n(건너뜀: {skipCount}개)";
        Debug.Log($"[Phase 7.2] {msg}");
        EditorUtility.DisplayDialog("Phase 7.2 Complete", msg, "OK");
    }


    /// <summary>
    /// [Phase 7.1-4] 최상단 + 모든 자식 GameObject을 재규적으로 순회하며
    /// Missing Script를 발본색원하는 력 클리닝 함수.
    /// </summary>
    private static void RemoveMissingScriptsRecursively(GameObject obj)
    {
        // 현재 오브젝트 Missing Script 제거
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

        // 모든 자식 오브젝트 순회하며 재규 호출
        foreach (Transform child in obj.transform)
        {
            RemoveMissingScriptsRecursively(child.gameObject);
        }
    }

/// <summary>
    /// [Phase 8.1] 지정된 폴더 안에서 첫 번째로 일치하는 AudioClip을 로드한다.
    /// filterKeyword가 있으면 파일명에 해당 키워드가 포함된 파일만 반환한다.
    /// </summary>
    private static AudioClip FindFirstAudioClip(string folderPath, string filterKeyword = null)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (filterKeyword != null)
            {
                string fileName = System.IO.Path.GetFileName(path);
                if (!fileName.Contains(filterKeyword)) continue;
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) return clip;
        }
        return null;
    }

}
