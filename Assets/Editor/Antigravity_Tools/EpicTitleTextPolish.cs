using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class EpicTitleTextPolish : EditorWindow
{
    [MenuItem("Tools/Valheim/Polish Epic Title Text")]
    public static void PolishEpicTitle()
    {
        // 1. 활성 씬에서 "TitleText" 객체 찾기
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TextMeshProUGUI titleTextObj = null;

        foreach (var txt in allTexts)
        {
            if (txt.gameObject.name.Equals("TitleText", System.StringComparison.OrdinalIgnoreCase))
            {
                titleTextObj = txt;
                break;
            }
        }

        if (titleTextObj == null)
        {
            Debug.LogError("[Epic Title Polish] Could not find any TextMeshProUGUI named 'TitleText'. Please open the Login Scene.");
            return;
        }

        Undo.RecordObject(titleTextObj, "Polish Epic Title Text");

        // 2. 단일 Color 속성을 에픽 그라데이션 컬러로 전면 교체
        titleTextObj.enableVertexGradient = true;

        // 발헤임 화로 불꽃 컬러 그라데이션 조합 생성
        // 위쪽: 눈부신 황금색
        Color32 topColor = new Color32(255, 200, 0, 255);
        // 아래쪽: 심연의 검붉은 불꽃색
        Color32 bottomColor = new Color32(180, 20, 0, 255);

        VertexGradient epicGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);
        titleTextObj.colorGradient = epicGradient;

        // 3. 자간 스케일링 (Character Spacing) 확장을 통한 영화 타이틀 느낌 부여
        titleTextObj.characterSpacing = 15f;

        // 4. 폰트 크기 1.2배 상향
        titleTextObj.fontSize *= 1.2f;

        // 명확성을 위해 정렬 상태 점검 (보통 가운데 정렬 권장)
        titleTextObj.alignment = TextAlignmentOptions.Center;

        // 5. 렌더 변경사항 씬 저장 마킹
        EditorUtility.SetDirty(titleTextObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<color=orange>[Epic Title Polish Success]</color> Epic visual parameters successfully injected to '{titleTextObj.text}'!");
    }
}
