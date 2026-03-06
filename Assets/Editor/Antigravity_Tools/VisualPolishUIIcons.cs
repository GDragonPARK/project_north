using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// [Phase 10.3] 건축 UI 및 인벤토리 슬롯의 시각적 불일치를 해결하고 디테일을 개선하는 에디터 툴.
/// </summary>
public class VisualPolishUIIcons : Editor
{
    [MenuItem("Tools/Valheim/Visual Polish UI")]
    public static void PolishUI()
    {
        int modifyCount = 0;

        // 1. [Building UI Visual Fix] - Panel_BuildingBar 탐색
        var panelBuildingBar = FindUIObject("Panel_BuildingBar");
        if (panelBuildingBar != null)
        {
            var buildImages = panelBuildingBar.GetComponentsInChildren<Image>(true);
            foreach (var img in buildImages)
            {
                // 부모 패널 자체의 백그라운드나 구분선 제외 (이름으로 필터링)
                if (img.gameObject.name.ToLower().Contains("panel") || img.gameObject.name.ToLower().Contains("bg"))
                    continue;

                // Color를 White로 (가시성 확보)
                if (img.color != Color.white)
                {
                    img.color = Color.white;
                    modifyCount++;
                }

                // Sliced로 변경 가설 (Sprite가 보더 값을 가지고 있다고 가정)
                // Sprite가 없는 경우 Sliced를 적용할 수 없음에 주의
                if (img.sprite != null && img.type != Image.Type.Sliced)
                {
                    // sprite가 boder를 가지고 있어야 sliced가 동작하지만 강제로 올려둠
                    img.type = Image.Type.Sliced;
                    modifyCount++;
                }

                EditorUtility.SetDirty(img);
                EditorUtility.SetDirty(img.gameObject);
            }
            Debug.Log($"[VisualPolishUIIcons] Panel_BuildingBar 디테일 업 완료.");
        }
        else
        {
            Debug.LogWarning("[VisualPolishUIIcons] Panel_BuildingBar를 찾지 못했습니다.");
        }

        // 2. [Inventory UI Detail Up] - Inventory_Panel 탐색
        var inventoryPanel = FindUIObject("Inventory_Panel");
        if (inventoryPanel == null)
        {
            // Canvas 밑을 뒤져서 비슷하게라도 찾기
            inventoryPanel = FindUIObject("Panel_Inventory");
        }

        if (inventoryPanel != null)
        {
            var slotImages = inventoryPanel.GetComponentsInChildren<Image>(true);
            
            // Frame / Border / Slot_mask 스프라이트 검색
            string[] guids = AssetDatabase.FindAssets("t:Texture2D frame OR t:Texture2D border OR t:Texture2D slot_bg OR t:Texture2D slot OR t:Texture2D box");
            Sprite frameSprite = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().Contains("frame") || path.ToLower().Contains("border") || path.ToLower().Contains("slot"))
                {
                    frameSprite = EnsureSpriteConversion(path);
                    if (frameSprite != null) break;
                }
            }

            foreach (var img in slotImages)
            {
                // "Slot (*)" 이름 형식의 슬롯 배경 이미지를 타겟 (자식 아이콘 제외)
                if (img.gameObject.name.ToLower().Contains("slot") || img.gameObject.name.ToLower().Contains("bg"))
                {
                    // Frame 할당
                    if (frameSprite != null && img.sprite != frameSprite)
                    {
                        img.sprite = frameSprite;
                        modifyCount++;
                    }

                    // 하얀색 및 Sliced
                    if (img.color != Color.white)
                    {
                        img.color = Color.white;
                        modifyCount++;
                    }

                    if (img.sprite != null && img.type != Image.Type.Sliced)
                    {
                        img.type = Image.Type.Sliced;
                        modifyCount++;
                    }

                    EditorUtility.SetDirty(img);
                    EditorUtility.SetDirty(img.gameObject);
                }
            }

            // [폰트 디테일 향상]
            var texts = inventoryPanel.GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                // 볼드 처리
                if (txt.fontStyle != FontStyle.Bold)
                {
                    txt.fontStyle = FontStyle.Bold;
                    modifyCount++;
                    EditorUtility.SetDirty(txt);
                    EditorUtility.SetDirty(txt.gameObject);
                }
            }

            Debug.Log($"[VisualPolishUIIcons] Inventory_Panel 디테일 업 완료. (Frame Sprite: {(frameSprite ? frameSprite.name : "없음")})");
        }
        else
        {
            Debug.LogWarning("[VisualPolishUIIcons] Inventory_Panel을 찾지 못했습니다.");
        }

        if (modifyCount > 0)
        {
            // 씬 저장 마킹
            var currentScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
        }

        Debug.Log($"[VisualPolishUIIcons] UI 비주얼 폴리싱 완료! (총 {modifyCount}개 속성 변경 및 씬 마킹 처리됨)");
    }

    private static GameObject FindUIObject(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in canvases)
            {
                var tr = canvas.transform.FindChildRecursivePolish(name);
                if (tr != null) return tr.gameObject;
            }
        }
        return obj;
    }

    private static Sprite EnsureSpriteConversion(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
            // Border를 살짝 주어 Sliced가 의미있게 동작하도록 함
            if (importer.spriteBorder == Vector4.zero)
            {
                importer.spriteBorder = new Vector4(8, 8, 8, 8); // 임의의 9-slice 보더 값
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

// Transform 확장 메서드 중복 방지를 위해 이름을 다르게 함
public static class TransformExtensionsPolish
{
    public static Transform FindChildRecursivePolish(this Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = child.FindChildRecursivePolish(childName);
            if (result != null) return result;
        }
        return null;
    }
}