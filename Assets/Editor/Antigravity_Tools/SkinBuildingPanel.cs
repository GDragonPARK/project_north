using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// [Phase 10.4] 건축 메인 캔버스의 시커먼 배경 판넬을 수집하여
/// 프로젝트 내의 고급 나무 판넬(Wood Skin)로 전면 교체하는 에디터 스크립트.
/// </summary>
public class SkinBuildingPanel : Editor
{
    [MenuItem("Tools/Valheim/Skin Building Panel")]
    public static void SkinPanel()
    {
        // 1. Canvas_Building 탐색
        GameObject canvasObj = GameObject.Find("Canvas_Building");
        if (canvasObj == null)
        {
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.name.Contains("Canvas_Building"))
                {
                    canvasObj = canvas.gameObject;
                    break;
                }
            }
        }

        if (canvasObj == null)
        {
            Debug.LogError("[SkinBuildingPanel] 씬에서 'Canvas_Building'을 찾을 수 없습니다.");
            return;
        }

        int modifyCount = 0;

        // 2. 우드 스킨 스프라이트 찾기 (woodpanel_feedback, woodpanel_crafting 등)
        string[] guids = AssetDatabase.FindAssets("t:Texture2D woodpanel_feedback OR t:Texture2D woodpanel_crafting OR t:Texture2D ui_wood_board OR t:Texture2D wood_bg");
        Sprite woodSkinSprite = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("woodpanel") || path.ToLower().Contains("wood_bg") || path.ToLower().Contains("ui_wood"))
            {
                woodSkinSprite = EnsureSpriteConversion(path);
                if (woodSkinSprite != null) break;
            }
        }

        if (woodSkinSprite == null)
        {
            Debug.LogWarning("[SkinBuildingPanel] 적용할 나무 판넬 스프라이트를 프로젝트 내에서 찾지 못했습니다.");
        }

        // 3. 메인 배경 판넬 (Construction) 탐색
        Transform constructionTra = canvasObj.transform.Find("Construction");
        if (constructionTra == null)
        {
            // 이름 기반으로 하위에서 한 번 더 검색
            var allChildTransforms = canvasObj.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildTransforms)
            {
                if (t.name == "Construction")
                {
                    constructionTra = t;
                    break;
                }
            }
        }

        if (constructionTra == null)
        {
            Debug.LogError("[SkinBuildingPanel] 'Canvas_Building' 하위에 'Construction' 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Image mainImage = constructionTra.GetComponent<Image>();
        if (mainImage == null)
        {
            // 수동으로 배경 오브젝트를 만드셨을 경우 대비 (하위 Image 중 가장 거대한 것)
            Image[] childImages = constructionTra.GetComponentsInChildren<Image>(true);
            float maxSize = -1f;
            foreach (var img in childImages)
            {
                RectTransform rt = img.rectTransform;
                float size = rt.rect.width * rt.rect.height;
                if (size > maxSize)
                {
                    maxSize = size;
                    mainImage = img;
                }
            }
        }

        if (mainImage != null)
        {
            if (woodSkinSprite != null && mainImage.sprite != woodSkinSprite)
            {
                mainImage.sprite = woodSkinSprite;
            }
            
            // Color를 White로 하여 텍스처 본연의 퀄리티(발헤임 감성)를 살림
            if (mainImage.color != Color.white)
            {
                mainImage.color = Color.white;
            }

            // 모서리 깨짐 방지
            if (mainImage.sprite != null && mainImage.type != Image.Type.Sliced)
            {
                mainImage.type = Image.Type.Sliced;
            }

            EditorUtility.SetDirty(mainImage);
            EditorUtility.SetDirty(mainImage.gameObject);
            modifyCount++;
            Debug.Log($"[SkinBuildingPanel] 진짜 메인 배경 판넬 스킨 교체 완료: {mainImage.gameObject.name}");
        }
        else
        {
            Debug.LogError("[SkinBuildingPanel] 'Construction' 하위에서 Image 컴포넌트를 찾을 수 없습니다.");
        }

        // 4. 잘못 스킨이 씌워졌던 Panel_BuildingBar 초기화/투명화 처리
        Transform buildingBarTra = constructionTra.Find("Panel_BuildingBar");
        if (buildingBarTra == null)
        {
            var trs = constructionTra.GetComponentsInChildren<Transform>(true);
            foreach (var t in trs)
            {
                if (t.name.Contains("Panel_BuildingBar"))
                {
                    buildingBarTra = t;
                    break;
                }
            }
        }

        if (buildingBarTra != null)
        {
            Image barImage = buildingBarTra.GetComponent<Image>();
            if (barImage != null)
            {
                // 투명하게 처리하여 겹치지 않게 함
                barImage.sprite = null;
                barImage.color = Color.clear;
                EditorUtility.SetDirty(barImage);
                EditorUtility.SetDirty(barImage.gameObject);
                Debug.Log($"[SkinBuildingPanel] 오지정되었던 버튼 바({buildingBarTra.name}) 초기화(투명화) 완료.");
            }
        }

        if (modifyCount > 0)
        {
            // 씬 저장 마킹
            var currentScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log($"[SkinBuildingPanel] 총 {modifyCount}개 판넬에 최고급 우드 스킨 적용 완료!");
        }
        else
        {
            Debug.Log("[SkinBuildingPanel] 변경할 대상 판넬을 찾지 못했습니다.");
        }
    }

    private static Sprite EnsureSpriteConversion(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
            if (importer.spriteBorder == Vector4.zero)
            {
                importer.spriteBorder = new Vector4(24, 24, 24, 24); // 넉넉한 9-slice 보더
                needsReimport = true;
            }
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}