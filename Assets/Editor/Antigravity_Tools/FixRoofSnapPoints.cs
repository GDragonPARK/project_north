using UnityEngine;
using UnityEditor;

public class FixRoofSnapPoints : EditorWindow
{
    [MenuItem("Tools/Valheim/Phase 10.8 - Fix Roof Snap Points")]
    public static void ShowWindow()
    {
        GetWindow<FixRoofSnapPoints>("Fix Roof Snap Points");
    }

    private void OnGUI()
    {
        GUILayout.Label("Align Roof Snap Points to Visuals", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("This will reparent Roof_Top, Roof_Bot, Roof_L, Roof_R to the 'Visuals' child object in WoodRoof_30 and WoodRoof_45 prefabs and reset their local coordinates.", MessageType.Warning);

        if (GUILayout.Button("Align Snap Points"))
        {
            AlignSnapPoints();
        }
    }

    private void AlignSnapPoints()
    {
        string[] searchPaths = new string[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("WoodRoof_ t:Prefab", searchPaths);
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // 타겟 프리팹 필터링
            if (assetName == "WoodRoof_30_Ghost" || assetName == "WoodRoof_30_Real" ||
                assetName == "WoodRoof_45_Ghost" || assetName == "WoodRoof_45_Real")
            {
                // [Phase 10.8-1 핫픽스] LoadPrefabContents 사용으로 Prefab Instance Reparenting 에러 원천 차단
                GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
                
                if (contentsRoot != null)
                {
                    Transform rootTransform = contentsRoot.transform;
                    Transform visualsTransform = rootTransform.Find("Visuals");

                    if (visualsTransform != null)
                    {
                        bool prefabModified = false;

                        // 처리할 스냅 포인트 목록 및 새 로컬 위치 정의
                        // 이름기준: Top, Bot, L, R
                        var targetSnapPoints = new (string name, Vector3 localPos, Vector3 localEuler)[]
                        {
                            ("Roof_Top", new Vector3(0, 0, 1.5f), new Vector3(0, 0, 0)),
                            ("Roof_Bot", new Vector3(0, 0, -1.5f), new Vector3(0, 180, 0)),
                            ("Roof_L", new Vector3(-1.5f, 0, 0), new Vector3(0, -90, 0)),
                            ("Roof_R", new Vector3(1.5f, 0, 0), new Vector3(0, 90, 0))
                        };

                        foreach (var target in targetSnapPoints)
                        {
                            // 먼저 기존 Root 밑에 있는지 찾음
                            Transform snapPoint = rootTransform.Find(target.name);
                            
                            // 혹시 이미 Visuals 밑에 있는지 체크
                            if (snapPoint == null)
                            {
                                snapPoint = visualsTransform.Find(target.name);
                            }

                            if (snapPoint != null)
                            {
                                // 부모를 Visuals로 변경
                                if (snapPoint.parent != visualsTransform)
                                {
                                    snapPoint.SetParent(visualsTransform, false); // false로 해야 localPosition이 초기화되거나 새 부모 기준으로 깔끔하게 들어감
                                }
                                
                                // 타겟 로컬 좌표로 강제 셋팅 (3x3 바닥 기준 모서리)
                                snapPoint.localPosition = target.localPos;
                                snapPoint.localEulerAngles = target.localEuler;
                                
                                prefabModified = true;
                            }
                            else
                            {
                                Debug.LogWarning($"[Roof Fix] {target.name} not found in {assetName}");
                            }
                        }

                        if (prefabModified)
                        {
                            // 변경사항을 에셋에 덮어쓰기 저장
                            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
                            modifiedCount++;
                            Debug.Log($"[Roof Fix] Successfully reparented and aligned snap points for {assetName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Roof Fix] 'Visuals' child not found in {assetName}. Skipping.");
                    }

                    // [Phase 10.8-1 핫픽스] 메모리 반환
                    PrefabUtility.UnloadPrefabContents(contentsRoot);
                }
            }
        }

        if (modifiedCount > 0)
        {
            Debug.Log($"<color=green>[Success]</color> Aligned snap points in {modifiedCount} roof prefabs.");
        }
        else
        {
            Debug.LogWarning("[Roof Fix] No snap points needed fixing or none were found.");
        }
    }
}
