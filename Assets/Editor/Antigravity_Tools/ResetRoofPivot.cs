using UnityEngine;
using UnityEditor;

public class ResetRoofPivot : EditorWindow
{
    [MenuItem("Tools/Valheim/Phase 10.7 - Reset Roof Pivot")]
    public static void ShowWindow()
    {
        GetWindow<ResetRoofPivot>("Reset Roof Pivot");
    }

    private void OnGUI()
    {
        GUILayout.Label("Reset Roof Visuals LocalPosition", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("This will reset the localPosition of the 'Visuals' child object in all WoodRoof_30 and WoodRoof_45 prefabs (Ghost and Real) to Vector3.zero.", MessageType.Info);

        if (GUILayout.Button("Reset Roof Pivots"))
        {
            ResetPivots();
        }
    }

    private void ResetPivots()
    {
        string[] searchPaths = new string[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("WoodRoof_ t:Prefab", searchPaths);
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // 타겟 프리팹만 필터링
            if (assetName == "WoodRoof_30_Ghost" || assetName == "WoodRoof_30_Real" ||
                assetName == "WoodRoof_45_Ghost" || assetName == "WoodRoof_45_Real")
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    Transform visualsTransform = prefab.transform.Find("Visuals");
                    if (visualsTransform != null)
                    {
                        // 프리팹의 내용을 변경하기 위해 편집 모드로 인스턴스화
                        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        Transform instanceVisuals = prefabInstance.transform.Find("Visuals");

                        if (instanceVisuals != null && instanceVisuals.localPosition != Vector3.zero)
                        {
                            Undo.RecordObject(instanceVisuals, "Reset Roof Pivot");
                            instanceVisuals.localPosition = Vector3.zero;
                            
                            // 변경사항을 프리팹 자산에 역방향으로 적용
                            PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
                            modifiedCount++;
                            Debug.Log($"[Roof Fix] Reset localPosition of Visuals in {assetName} to Vector3.zero");
                        }
                        
                        // 임시 인스턴스는 씬에서 제거
                        DestroyImmediate(prefabInstance);
                    }
                    else
                    {
                        Debug.LogWarning($"[Roof Fix] No 'Visuals' child found in {assetName}");
                    }
                }
            }
        }

        if (modifiedCount > 0)
        {
            Debug.Log($"<color=green>[Success]</color> Successfully reset {modifiedCount} roof prefabs.");
        }
        else
        {
            Debug.LogWarning("[Roof Fix] No target roof prefabs needed fixing or none were found.");
        }
    }
}
