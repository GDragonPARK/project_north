using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabSnapSetupTool : EditorWindow
{
    private static readonly string[] RequiredTags = { "SnapVolume", "SnapPoint" };
    
    // 타겟이 될 프리팹 경로 추가 시 배열에 등록
    private static readonly string[] TargetPrefabPaths = {
        "Assets/Resources/BuildingData/Wood_Floor_Prefab.prefab"
    };

    [MenuItem("Tools/Project North/Setup Floor Snap Volumes")]
    public static void SetupFloorSnapVolumes()
    {
        EnsureTagsExist();

        int successCount = 0;
        foreach (string path in TargetPrefabPaths)
        {
            if (ProcessPrefab(path))
            {
                successCount++;
            }
        }

        if (successCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PrefabSnapSetup] 성공적으로 {successCount}개의 타겟 프리팹에 SnapVolume을 세팅했습니다!");
        }
        else
        {
            Debug.LogWarning("[PrefabSnapSetup] 처리된 프리팹이 없습니다. 에러나 누락된 경로를 확인하세요.");
        }
    }

    private static void EnsureTagsExist()
    {
        // TagManager 열어 필요한 태그가 있는지 확인 (코드 생성 자동화 생략 및 안내만 출력)
        // 현 프로젝트 셋팅상 이미 존재한다고 전제
    }

    private static bool ProcessPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PrefabSnapSetup] 프래팹을 찾을 수 없습니다: {prefabPath}");
            return false;
        }

        // 프리팹 에셋 내부 인스턴스를 직접 수정(최신 Unity의 PrefabUtility 패턴)
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editingScope.prefabContentsRoot;
            
            // 기존 SnapVolume 삭제 (멱등성 확보)
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("SnapVolume_"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (GameObject obj in toDestroy)
            {
                DestroyImmediate(obj);
            }

            // 부모의 BoxCollider를 찾아 바닥 크기 가늠
            BoxCollider baseCol = root.GetComponentInChildren<BoxCollider>();
            if (baseCol == null)
            {
                Debug.LogError($"[PrefabSnapSetup] BoxCollider가 존재하지 않습니다: {prefab.name}");
                return false;
            }

            Vector3 c = baseCol.center;
            Vector3 s = baseCol.size;
            Vector3 ext = s * 0.5f;

            // 투명 트리거의 두께 정보 및 볼륨 너비
            float triggerThickness = 0.2f;  // 충분히 감지될 두께
            float triggerHeight    = 0.2f;

            float topY = c.y + ext.y;
            Vector3 volSize = new Vector3(s.x, triggerHeight, triggerThickness);

            // 북/남 (Z축 모서리), 동/서 (X축 모서리)
            CreateSnapVolume(root, "SnapVolume_N", new Vector3(c.x, topY, c.z + ext.z), Quaternion.Euler(0, 0, 0), volSize); // 북
            CreateSnapVolume(root, "SnapVolume_S", new Vector3(c.x, topY, c.z - ext.z), Quaternion.Euler(0, 180, 0), volSize); // 남
            CreateSnapVolume(root, "SnapVolume_E", new Vector3(c.x + ext.x, topY, c.z), Quaternion.Euler(0, 90, 0), volSize); // 동
            CreateSnapVolume(root, "SnapVolume_W", new Vector3(c.x - ext.x, topY, c.z), Quaternion.Euler(0, -90, 0), volSize); // 서
            
            // 프리팹 저장 시 편집을 마침(using 종료)과 함께 자동 저장됨
            Debug.Log($"[PrefabSnapSetup] 프리팹 셋업 완료: {prefab.name}");
        }

        return true;
    }

    private static void CreateSnapVolume(GameObject parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 size)
    {
        GameObject volObj = new GameObject(name);
        volObj.transform.SetParent(parent.transform, false);
        volObj.transform.localPosition = localPosition;
        volObj.transform.localRotation = localRotation;
        
        try
        {
            volObj.tag = "SnapVolume";
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"[PrefabSnapSetup] 'SnapVolume' 태그가 존재하지 않아 할당 실패! 유니티 TagManager에 수동 추가 필수. Error: {e.Message}");
        }

        BoxCollider col = volObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
        
        // 투명 트리거이므로 MeshRenderer 생성 안함
    }
}
