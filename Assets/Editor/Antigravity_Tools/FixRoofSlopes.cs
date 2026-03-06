using UnityEngine;
using UnityEditor;

public class FixRoofSlopes : Editor
{
    [MenuItem("Tools/Valheim/Fix Roof Slopes")]
    public static void FixSlopes()
    {
        string[] prefabs = new string[]
        {
            "Assets/Prefabs/Building/WoodRoof_30_Ghost.prefab",
            "Assets/Prefabs/Building/WoodRoof_30_Real.prefab",
            "Assets/Prefabs/Building/WoodRoof_45_Ghost.prefab",
            "Assets/Prefabs/Building/WoodRoof_45_Real.prefab"
        };

        int count = 0;
        foreach (string path in prefabs)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root == null) continue;

                float angle = path.Contains("30") ? 30f : 45f;

                // 1. 기존의 Root 스케일 백업
                Vector3 oldScale = root.transform.localScale;
                
                Transform visualChild = root.transform.Find("Visuals");
                bool needsRestructure = (visualChild == null && root.GetComponent<MeshFilter>() != null);
                
                if (needsRestructure)
                {
                    visualChild = new GameObject("Visuals").transform;
                    visualChild.SetParent(root.transform, false);

                    // Root의 비주얼 컴포넌트들을 Visuals로 이동
                    MeshFilter mf = root.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        var newMf = visualChild.gameObject.AddComponent<MeshFilter>();
                        newMf.sharedMesh = mf.sharedMesh;
                        DestroyImmediate(mf, true);
                    }

                    MeshRenderer mr = root.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        var newMr = visualChild.gameObject.AddComponent<MeshRenderer>();
                        newMr.sharedMaterials = mr.sharedMaterials;
                        DestroyImmediate(mr, true);
                    }

                    BoxCollider bc = root.GetComponent<BoxCollider>();
                    if (bc != null)
                    {
                        var newBc = visualChild.gameObject.AddComponent<BoxCollider>();
                        newBc.center = bc.center;
                        newBc.size = bc.size;
                        newBc.isTrigger = bc.isTrigger;
                        DestroyImmediate(bc, true);
                    }

                    // 모든 기존 자식들을 보존하며 로컬 위치를 oldScale에 곱해줍니다. 
                    // SnapPoint 등의 위치가 세계 좌표계상 동일하게 유지되도록 보정
                    foreach (Transform child in root.transform)
                    {
                        if (child == visualChild) continue;
                        Vector3 pos = child.localPosition;
                        pos.x *= oldScale.x;
                        pos.y *= oldScale.y;
                        pos.z *= oldScale.z;
                        child.localPosition = pos;
                    }

                    // Root 리셋 ("스냅 위치 유지를 위해 0,0,0 고정")
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    root.transform.localScale = Vector3.one;

                    // Visuals에 스케일과 피벗/회전 적용
                    visualChild.localScale = oldScale; // 예: 3, 0.25, 3
                }
                
                if (visualChild != null)
                {
                    // 최상위 Transform은 스냅 위치 유지를 위해 0,0,0으로 강제
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    root.transform.localScale = Vector3.one;

                    // X축 기울이기
                    visualChild.localRotation = Quaternion.Euler(angle, 0f, 0f);

                    // (피벗 조정) 
                    // Y/Z 값을 살짝 조정하여 부모 피벗이 지붕의 끝자락(하단)에 오도록 맞춤
                    Vector3 pivotOffset = new Vector3(0f, 0f, -0.5f); // Mesh의 하단 엣지 (로컬 Z 기준)
                    pivotOffset.x *= visualChild.localScale.x;
                    pivotOffset.y *= visualChild.localScale.y;
                    pivotOffset.z *= visualChild.localScale.z;
                    
                    Vector3 rotatedOffset = visualChild.localRotation * pivotOffset;
                    visualChild.localPosition = -rotatedOffset;
                }

                count++;
            }
        }

        Debug.Log($"[FixRoofSlopes] {count}개의 지붕 프리팹(Ghost/Real) 성형수술 완료! 메쉬 각도 30/45도 달성.");
    }
}
