using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class StatsUIVisualPolish : EditorWindow
{
    [MenuItem("Tools/Valheim/Polish Stats UI")]
    public static void PolishUI()
    {
        // 1. Slider나 Canvas 이름 의존성을 전부 폐기합니다.
        // 씬 내의 모든 렌더러블 Image 컴포넌트를 직접 수집합니다.
        Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allImages == null || allImages.Length == 0)
        {
            Debug.LogWarning("[Stats Polish] No Image components found in the current scene.");
            return;
        }

        int modifiedCount = 0;

        // 2. 우드 프레임용 스프라이트 로드 (Background 씌우기 용도)
        Sprite frameSprite = null;
        string[] frameGuids = AssetDatabase.FindAssets("woodpanel_ t:Sprite");
        if (frameGuids.Length > 0)
        {
            frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(frameGuids[0]));
        }

        // 3. 수석 개발자 지정 명칭에 따른 정확한 타겟팅 및 스킨 주입
        foreach (Image img in allImages)
        {
            string objName = img.gameObject.name;
            bool polished = false;

            // [배경 프레임 처리]
            if (objName == "HealthBar_BG" || objName == "StaminaBar_BG")
            {
                img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 어두운 반투명 검정
                if (frameSprite != null)
                {
                    img.sprite = frameSprite;
                    img.type = Image.Type.Sliced;
                }
                polished = true;
            }
            // [체력 채우기 처리]
            else if (objName == "HealthBar_Fill")
            {
                img.color = new Color32(220, 20, 20, 255); // 진홍색 (핏빛)
                polished = true;
            }
            // [스태미나 채우기 처리]
            else if (objName == "StaminaBar_Fill")
            {
                img.color = new Color32(255, 200, 0, 255); // 생동감 있는 황금색
                polished = true;
            }

            if (polished)
            {
                modifiedCount++;
                Debug.Log($"[Stats Polish] Successfully polished custom UI image: {objName}");
            }
        }

        // 4. (선택) 텍스트 스타일 통일화 (기존 텍스트 스캔 유지)
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in allTexts)
        {
            string txtName = txt.gameObject.name.ToLower();
            if (txtName.Contains("health") || txtName.Contains("hp") || txtName.Contains("stamina") || txtName.Contains("sp"))
            {
                txt.fontStyle |= FontStyles.Bold;
                modifiedCount++;
            }
        }

        // 5. 결과 저장 (Dirty Mark)
        if (modifiedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=orange>[Stats UI Polish Success]</color> Applied {modifiedCount} visual modifications cleanly via Custom Image detection.");
        }
        else
        {
            Debug.LogWarning("[Stats Polish] No exact match found for HealthBar_BG/Fill or StaminaBar_BG/Fill. Please check inspector names.");
        }
    }
}
