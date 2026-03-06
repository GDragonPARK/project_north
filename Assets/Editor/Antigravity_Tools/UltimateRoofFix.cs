using UnityEngine;
using UnityEditor;

public class UltimateRoofFix : EditorWindow
{
    [MenuItem("Tools/Valheim/Ultimate Roof Fix")]
    public static void ApplyFix()
    {
        string[] searchPaths = new string[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("WoodRoof_ t:Prefab", searchPaths);
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (assetName == "WoodRoof_30_Ghost" || assetName == "WoodRoof_30_Real" ||
                assetName == "WoodRoof_45_Ghost" || assetName == "WoodRoof_45_Real")
            {
                GameObject contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
                if (contentsRoot != null)
                {
                    Transform rootTransform = contentsRoot.transform;

                    // 1. 타겟 객체 수집
                    Transform visuals = FindDeepChild(rootTransform, "Visuals");
                    Transform roofTop = FindDeepChild(rootTransform, "Roof_Top");
                    Transform roofBot = FindDeepChild(rootTransform, "Roof_Bot");
                    Transform roofL = FindDeepChild(rootTransform, "Roof_L");
                    Transform roofR = FindDeepChild(rootTransform, "Roof_R");

                    // 2. 계층 평탄화 (Anchor 삭제)
                    if (visuals) visuals.SetParent(rootTransform, false);
                    if (roofTop) roofTop.SetParent(rootTransform, false);
                    if (roofBot) roofBot.SetParent(rootTransform, false);
                    if (roofL)   roofL.SetParent(rootTransform, false);
                    if (roofR)   roofR.SetParent(rootTransform, false);

                    Transform anchor = rootTransform.Find("Anchor");
                    if (anchor != null)
                    {
                        DestroyImmediate(anchor.gameObject);
                    }

                    bool is45 = assetName.Contains("45");

                    // 3. 최종 절대 좌표 및 리버스 피치 주입
                    if (is45)
                    {
                        // Visuals: -45도 (Up-Pitch) 보정
                        if (visuals) { visuals.localScale = new Vector3(3f, 0.2f, 3f); visuals.localPosition = new Vector3(0f, 1.1313f, 0.9899f); visuals.localEulerAngles = new Vector3(-45f, 0f, 0f); }
                        // SnapPoints: 수평(0도) 글로벌 정렬 유지 (이중회전 원천차단)
                        if (roofTop) { roofTop.localScale = Vector3.one; roofTop.localPosition = new Vector3(0f, 2.1213f, 2.1213f); roofTop.localEulerAngles = Vector3.zero; }
                        if (roofBot) { roofBot.localScale = Vector3.one; roofBot.localPosition = Vector3.zero; roofBot.localEulerAngles = new Vector3(0, 180, 0); }
                        if (roofL)   { roofL.localScale = Vector3.one; roofL.localPosition = new Vector3(-1.5f, 1.1313f, 0.9899f); roofL.localEulerAngles = new Vector3(0, -90, 0); }
                        if (roofR)   { roofR.localScale = Vector3.one; roofR.localPosition = new Vector3(1.5f, 1.1313f, 0.9899f); roofR.localEulerAngles = new Vector3(0, 90, 0); }
                    }
                    else
                    {
                        // 30도 프리팹 보정
                        if (visuals) { visuals.localScale = new Vector3(3f, 0.2f, 3f); visuals.localPosition = new Vector3(0f, 0.8366f, 1.2490f); visuals.localEulerAngles = new Vector3(-30f, 0f, 0f); }
                        if (roofTop) { roofTop.localScale = Vector3.one; roofTop.localPosition = new Vector3(0f, 1.5f, 2.5980f); roofTop.localEulerAngles = Vector3.zero; }
                        if (roofBot) { roofBot.localScale = Vector3.one; roofBot.localPosition = Vector3.zero; roofBot.localEulerAngles = new Vector3(0, 180, 0); }
                        if (roofL)   { roofL.localScale = Vector3.one; roofL.localPosition = new Vector3(-1.5f, 0.8366f, 1.2490f); roofL.localEulerAngles = new Vector3(0, -90, 0); }
                        if (roofR)   { roofR.localScale = Vector3.one; roofR.localPosition = new Vector3(1.5f, 0.8366f, 1.2490f); roofR.localEulerAngles = new Vector3(0, 90, 0); }
                    }

                    // 저장 및 메모리 해제
                    PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
                    PrefabUtility.UnloadPrefabContents(contentsRoot);
                    modifiedCount++;
                    Debug.Log($"[Ultimate Roof Fix] Final Flat Hierarchy applied to {assetName}");
                }
            }
        }

        if (modifiedCount > 0)
            Debug.Log($"<color=cyan>[Final Flat Hierarchy Success]</color> Applied to {modifiedCount} prefabs.");
        else
            Debug.LogWarning("[Ultimate Roof Fix] No target prefabs found.");
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
