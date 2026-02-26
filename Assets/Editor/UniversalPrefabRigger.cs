using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UniversalPrefabRigger : EditorWindow
{
    // 벽 소켓의 강제 트랜스폼 값 (스케일 (3, 3, 0.25) 역산: Y:-1.5/3=-0.5, Z:-0.1/0.25=-0.4)
    private static readonly Vector3 WALL_ROOT_LOCAL_POS = new Vector3(0f, -0.5f, -0.4f);

    // 삭제 대상 소켓 이름 목록
    private static readonly string[] JUNK_SOCKET_NAMES = {
        "PlacementAnchor", "Wall_Bot", "Wall_Top", "Wall_L", "Wall_R",
        "RootSocket", "SnapPoint", "SnapVolume"
    };

    [MenuItem("Tools/Project North/Rig All Building Sockets")]
    public static void RigAllBuildingSockets()
    {
        int floorSuccess = 0;
        int wallSuccess = 0;

        // 동적 에셋 검색: 이름에 "WoodFloor" 또는 "WoodWall"이 포함된 프리팹
        string[] floorGuids = AssetDatabase.FindAssets("WoodFloor t:Prefab");
        string[] floorGuids2 = AssetDatabase.FindAssets("Wood_Floor t:Prefab"); // Wood_Floor_Prefab 대응
        string[] wallGuids = AssetDatabase.FindAssets("WoodWall t:Prefab");

        HashSet<string> floorPaths = new HashSet<string>();
        foreach (string guid in floorGuids) floorPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
        foreach (string guid in floorGuids2) floorPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

        HashSet<string> wallPaths = new HashSet<string>();
        foreach (string guid in wallGuids) wallPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string path in floorPaths)
        {
            if (ProcessFloorPrefab(path)) floorSuccess++;
        }

        foreach (string path in wallPaths)
        {
            if (ProcessWallPrefab(path)) wallSuccess++;
        }

        if (floorSuccess > 0 || wallSuccess > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Universal Rigger] 완료: Floor {floorSuccess}개, Wall {wallSuccess}개 파생 프리팹 일괄 리셋 & 소켓 교정 완료!");
        }
        else
        {
            Debug.LogWarning("[Universal Rigger] 검색된 프리팹이 없습니다.");
        }
    }

    private static bool ProcessFloorPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return false;

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editingScope.prefabContentsRoot;

            // [긴급 치유] 67개의 저장 에러 원인(Missing Script) 일괄 소거
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removedCount > 0) Debug.Log($"[Cleanup] {prefab.name} - 제거된 깨진 스크립트 수: {removedCount}");

            // [핵심] 부모 트랜스폼 오염 강제 리셋 (Ghost/Real 분화 시 발생한 Y축 변형 등 씻어냄)
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            // 기존 소켓/볼륨 전체 삭제 (멱등성)
            DeleteJunkChildren(root);

            BoxCollider baseCol = root.GetComponentInChildren<BoxCollider>();
            if (baseCol == null)
            {
                Debug.LogError($"[Floor Rigger] BoxCollider 누락: {prefab.name}");
                return false;
            }

            Vector3 c = baseCol.center;
            Vector3 ext = baseCol.size * 0.5f;
            float topY = c.y + ext.y;

            // 4면 윗면 가장자리 정중앙: Forward(Z축)가 반드시 바깥쪽을 향하도록 세팅
            CreateSocket(root, "SnapPoint", new Vector3(c.x, topY, c.z + ext.z), Quaternion.Euler(0, 0, 0));     // 북(N)
            CreateSocket(root, "SnapPoint", new Vector3(c.x + ext.x, topY, c.z), Quaternion.Euler(0, 90, 0));    // 동(E)
            CreateSocket(root, "SnapPoint", new Vector3(c.x, topY, c.z - ext.z), Quaternion.Euler(0, 180, 0));   // 남(S)
            CreateSocket(root, "SnapPoint", new Vector3(c.x - ext.x, topY, c.z), Quaternion.Euler(0, -90, 0));   // 서(W)

            Debug.Log($"[Floor Rigger] 교정 완료: {prefab.name}");
        }
        return true;
    }

    private static bool ProcessWallPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return false;

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editingScope.prefabContentsRoot;

            // [긴급 치유] 67개의 저장 에러 원인(Missing Script) 일괄 소거
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            if (removedCount > 0) Debug.Log($"[Cleanup] {prefab.name} - 제거된 깨진 스크립트 수: {removedCount}");

            // [핵심] 부모 트랜스폼 오염 강제 리셋
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            // 기존 소켓 전부 삭제 (PlacementAnchor, Wall_Bot 등 포함)
            DeleteJunkChildren(root);

            // 유일한 소켓: RootSocket (수석 개발자 지시 값으로 강제 세팅)
            CreateSocket(root, "RootSocket", WALL_ROOT_LOCAL_POS, Quaternion.identity);

            Debug.Log($"[Wall Rigger] 교정 완료: {prefab.name}");
        }
        return true;
    }

    private static void DeleteJunkChildren(GameObject root)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in root.transform)
        {
            foreach (string junkName in JUNK_SOCKET_NAMES)
            {
                if (child.name.StartsWith(junkName))
                {
                    toDestroy.Add(child.gameObject);
                    break;
                }
            }
        }
        foreach (GameObject obj in toDestroy) DestroyImmediate(obj);
    }

    private static void CreateSocket(GameObject parent, string name, Vector3 localPos, Quaternion localRot)
    {
        GameObject socketObj = new GameObject(name);
        socketObj.transform.SetParent(parent.transform, false);
        socketObj.transform.localPosition = localPos;
        socketObj.transform.localRotation = localRot;

        try { socketObj.tag = "SnapPoint"; }
        catch (UnityException e) { Debug.LogWarning($"[Rigger] 태그 할당 실패: {e.Message}"); }

        // 모든 소켓에 투명 SphereCollider 부착 (OverlapSphere 감지 보장)
        SphereCollider col = socketObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;
    }
}
