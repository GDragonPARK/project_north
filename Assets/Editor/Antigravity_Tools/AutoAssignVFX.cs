using UnityEditor;
using UnityEngine;

/// <summary>
/// [Phase 6.1-11] AI 자율 탐색: VFX_WoodHit.prefab을 건축물 Real 프리팹의
/// BuildingPiece.breakEffectPrefab에 일괄 자동 할당하는 에디터 툴.
/// </summary>
public static class AutoAssignVFX
{
    // ── AI가 탐색하여 확인한 경로 하드코딩 ──
    private const string VFX_PATH = "Assets/VFX_WoodHit.prefab";

    private static readonly string[] PREFAB_PATHS = new[]
    {
        "Assets/Prefabs/Building/WoodFloor_Real.prefab",
        "Assets/Prefabs/Building/WoodWall_Real.prefab",
        "Assets/Prefabs/Building/WoodRoof_30_Real.prefab",
        "Assets/Prefabs/Building/WoodRoof_45_Real.prefab",
    };

    [MenuItem("Tools/Valheim/Assign Break VFX")]
    public static void AssignBreakVFX()
    {
        // 1. VFX 프리팹 로드
        var vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VFX_PATH);
        if (vfxPrefab == null)
        {
            Debug.LogError($"[AutoAssignVFX] VFX 프리팹을 찾을 수 없습니다: {VFX_PATH}");
            EditorUtility.DisplayDialog("오류", $"VFX 프리팹을 찾을 수 없습니다:\n{VFX_PATH}", "확인");
            return;
        }

        int successCount = 0;
        int failCount    = 0;

        foreach (string path in PREFAB_PATHS)
        {
            // 2. 건축물 프리팹 로드
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[AutoAssignVFX] 프리팹을 찾을 수 없습니다: {path}");
                failCount++;
                continue;
            }

            // 3. Prefab 편집 모드로 열기
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root  = editScope.prefabContentsRoot;
                var piece = root.GetComponent<BuildingPiece>();

                if (piece == null)
                {
                    Debug.LogWarning($"[AutoAssignVFX] BuildingPiece 컴포넌트 없음: {path}");
                    failCount++;
                    continue;
                }

                piece.breakEffectPrefab = vfxPrefab;
                Debug.Log($"[AutoAssignVFX] ✅ 할당 완료: {prefabAsset.name} → {vfxPrefab.name}");
                successCount++;
            }
            // EditPrefabContentsScope가 using 블록 종료 시 자동으로 저장
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"VFX 자동 할당 완료!\n\n✅ 성공: {successCount}개\n❌ 실패: {failCount}개\n\nVFX: {vfxPrefab.name}";
        Debug.Log($"[AutoAssignVFX] {msg}");
        EditorUtility.DisplayDialog("AutoAssignVFX 완료", msg, "확인");
    }
}
