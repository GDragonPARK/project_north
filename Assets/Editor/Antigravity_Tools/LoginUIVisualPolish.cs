using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class LoginUIVisualPolish : EditorWindow
{
    [MenuItem("Tools/Valheim/Polish Login UI")]
    public static void PolishUI()
    {
        // 1. 활성 씬에서 Canvas_Login 찾기
        GameObject canvas = GameObject.Find("Canvas_Login");
        if (canvas == null)
        {
            Debug.LogError("[Login Polish] 'Canvas_Login' not found in the current scene. Please open the LoginScene.");
            return;
        }

        int modifiedCount = 0;

        // 2. [배경 투명화] BG_Overlay 색상 변경 (3D 환경 렌더링용)
        Transform bgOverlay = canvas.transform.Find("BG_Overlay");
        if (bgOverlay != null)
        {
            Image bgImage = bgOverlay.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.6f);
                modifiedCount++;
            }
        }

        // 3. [우드톤 패널 스킨 적용] ConnectPanel
        Transform connectPanel = canvas.transform.Find("ConnectPanel");
        if (connectPanel != null)
        {
            Image panelImage = connectPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                // 프로젝트 내의 우드 패널 스프라이트 검색 (예: woodpanel_crafting)
                string[] guids = AssetDatabase.FindAssets("woodpanel_ t:Sprite");
                if (guids.Length > 0)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (woodSprite != null)
                    {
                        panelImage.sprite = woodSprite;
                        panelImage.color = Color.white;
                        panelImage.type = Image.Type.Sliced;
                        modifiedCount++;
                    }
                }
                else
                {
                    Debug.LogWarning("[Login Polish] Wood panel sprite not found. Skipping panel skin.");
                }
            }

            // 4. [타이틀 텍스트 퀄리티 업] TitleGroup/TitleText
            Transform titleGroup = connectPanel.Find("TitleGroup");
            if (titleGroup != null)
            {
                Transform titleTextTrans = titleGroup.Find("TitleText");
                if (titleTextTrans != null)
                {
                    TextMeshProUGUI titleText = titleTextTrans.GetComponent<TextMeshProUGUI>();
                    if (titleText != null)
                    {
                        titleText.fontStyle |= FontStyles.Bold;
                        // 발헤임 불빛 오렌지색
                        titleText.color = new Color32(255, 140, 0, 255);
                        modifiedCount++;
                    }
                }
            }

            // 5. [입력창 및 버튼 다크우드 톤 다운]
            Transform[] targetAreas = new Transform[]
            {
                connectPanel.Find("InputArea"),
                connectPanel.Find("AuthInputArea"),
                connectPanel.Find("ButtonArea")
            };

            foreach (Transform area in targetAreas)
            {
                if (area != null)
                {
                    // 구역 내의 모든 Image 컴포넌트를 찾아 톤 다운 (음각 효과)
                    Image[] childImages = area.GetComponentsInChildren<Image>(true);
                    foreach (Image img in childImages)
                    {
                        // 텍스트나 다른 요소가 아닌 순수 배경 Image인 경우 처리
                        // (보통 InputField나 Button의 타겟 그래픽)
                        img.color = new Color(0f, 0f, 0f, 0.5f);
                        modifiedCount++;
                    }
                }
            }
        }

        // 6. 씬 더티 마킹 (변경사항 저장 유도)
        if (modifiedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=orange>[Login UI Polish Success]</color> Applied {modifiedCount} visual modifications cleanly without altering hierarchy.");
        }
        else
        {
            Debug.LogWarning("[Login Polish] No visual changes were applied. Check object names.");
        }
    }
}
