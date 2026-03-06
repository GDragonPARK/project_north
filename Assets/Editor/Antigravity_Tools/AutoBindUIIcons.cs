using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// [Phase 10.3] 건축 UI 버튼의 이미지와 버튼 컴포넌트를 자동 할당하고,
/// 프로젝트 내 아이콘(Texture)을 검색해 Sprite로 자동 변환 후 연결해주는 에디터 툴.
/// </summary>
public class AutoBindUIIcons : Editor
{
    [MenuItem("Tools/Valheim/Auto Bind UI Icons")]
    public static void BindUIIcons()
    {
        // 1. 씬에서 Panel_BuildingBar 찾기
        GameObject panelObj = GameObject.Find("Panel_BuildingBar");
        if (panelObj == null)
        {
            // Canvas 아래에 비활성화된 경우가 많으므로 리얼타임 검색 시도
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in canvases)
            {
                var tr = canvas.transform.FindChildRecursive("Panel_BuildingBar");
                if (tr != null)
                {
                    panelObj = tr.gameObject;
                    break;
                }
            }
        }

        if (panelObj == null)
        {
            Debug.LogError("[AutoBindUIIcons] 씬에서 'Panel_BuildingBar'를 찾을 수 없습니다.");
            return;
        }

        var buildButtons = panelObj.GetComponentsInChildren<BuildUIButton>(true);
        if (buildButtons.Length == 0)
        {
            Debug.LogWarning("[AutoBindUIIcons] Panel_BuildingBar 하위에 BuildUIButton 컴포넌트를 가진 버튼이 없습니다.");
            return;
        }

        int bindCount = 0;
        
        // 2. 프로젝트 내 아이콘 텍스쳐 수집 (간이 검색)
        string[] guids = AssetDatabase.FindAssets("t:Texture2D icon OR t:Texture2D wood OR t:Texture2D floor OR t:Texture2D wall OR t:Texture2D roof");
        
        Sprite defaultSprite = null;
        
        foreach (var btn in buildButtons)
        {
            // [참조 자동 할당]
            if (btn.button == null) btn.button = btn.GetComponent<Button>();
            
            // Image는 자신에게 있으면 그것, 아니면 자식을 탐색 (보통 자식에 실제 아이콘이 있음)
            if (btn.iconImage == null)
            {
                var imgs = btn.GetComponentsInChildren<Image>(true);
                foreach(var img in imgs)
                {
                    if (img.gameObject != btn.gameObject) // 자식을 우선시
                    {
                        btn.iconImage = img;
                        break;
                    }
                }
                if (btn.iconImage == null) btn.iconImage = btn.GetComponent<Image>();
            }

            // [아이콘 검색 및 적용]
            string targetKeyword = btn.gameObject.name.ToLower();
            targetKeyword = targetKeyword.Replace("slot_", "").Replace("btn_", ""); // "woodfloor", "woodwall" 등
            
            Sprite targetSprite = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.ToLower().Contains(targetKeyword))
                {
                    targetSprite = EnsureSpriteConversion(path);
                    if (targetSprite != null) break;
                }
            }

            // 못 찾았으면 wood가 들어간 아무거나 공통으로 시도
            if (targetSprite == null && defaultSprite == null)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.ToLower().Contains("wood") || path.ToLower().Contains("icon"))
                    {
                        defaultSprite = EnsureSpriteConversion(path);
                        if (defaultSprite != null) break;
                    }
                }
                targetSprite = defaultSprite;
            }
            else if (targetSprite == null)
            {
                targetSprite = defaultSprite;
            }

            if (targetSprite != null && btn.iconImage != null)
            {
                btn.iconImage.sprite = targetSprite;
            }

            EditorUtility.SetDirty(btn);
            EditorUtility.SetDirty(btn.gameObject);
            bindCount++;
            
            Debug.Log($"[AutoBindUIIcons] 바인딩 완료: {btn.gameObject.name} (아이콘: {(targetSprite ? targetSprite.name : "없음")})");
        }

        // 씬 저장 마킹
        var currentScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log($"[AutoBindUIIcons] 총 {bindCount}개의 빌드 버튼 업데이트 및 씬 저장 완료!");
    }

    private static Sprite EnsureSpriteConversion(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
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

// Transform 확장 메서드 (비활성화 대상 깊은 탐색용)
public static class TransformExtensions
{
    public static Transform FindChildRecursive(this Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = child.FindChildRecursive(childName);
            if (result != null) return result;
        }
        return null;
    }
}