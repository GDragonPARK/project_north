#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class BuildingSystem_Builder : MonoBehaviour
{
    [MenuItem("Antigravity/🏗️ Setup Building System (Step 1)")]
    public static void Setup()
    {
        // 1. 데이터 폴더 및 샘플 데이터 생성
        string dataPath = "Assets/Resources/BuildingData";
        if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
        
        // 카테고리 생성
        var category = AssetDatabase.LoadAssetAtPath<BuildingCategorySO>(dataPath + "/Construction.asset");
        if (category == null)
        {
            category = ScriptableObject.CreateInstance<BuildingCategorySO>();
            category.categoryName = "Construction";
            AssetDatabase.CreateAsset(category, dataPath + "/Construction.asset");
        }

        // 아이템 데이터 생성 (Wood Floor)
        var floorData = AssetDatabase.LoadAssetAtPath<BuildingDataSO>(dataPath + "/Wood_Floor_Data.asset");
        if (floorData == null)
        {
            floorData = ScriptableObject.CreateInstance<BuildingDataSO>();
            floorData.id = "wood_floor";
            floorData.displayName = "Wood Floor";
            floorData.category = category;
            
            // 임시 프리팹 (Cube) 생성해서 연결
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempCube.name = "Wood_Floor_Prefab";
            string prefabPath = "Assets/Resources/BuildingData/Wood_Floor_Prefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempCube, prefabPath);
            DestroyImmediate(tempCube);
            
            floorData.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            AssetDatabase.CreateAsset(floorData, dataPath + "/Wood_Floor_Data.asset");
        }
        AssetDatabase.SaveAssets();

        // 2. 매니저 설치
        var managerObj = GameObject.Find("System_Building");
        if (managerObj == null) managerObj = new GameObject("System_Building");
        
        var manager = managerObj.GetComponent<BuildingManager>();
        if (manager == null) manager = managerObj.AddComponent<BuildingManager>();

        // 3. UI 생성
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다! Canvas를 먼저 만들어주세요.");
            return;
        }

        // 패널
        var panelObj = canvas.transform.Find("Building_UI_Panel");
        if (panelObj == null)
        {
            GameObject pObj = new GameObject("Building_UI_Panel", typeof(RectTransform), typeof(Image));
            panelObj = pObj.transform;
            panelObj.SetParent(canvas.transform, false);
            
            // 전체 화면 채우기
            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 반투명 배경
            panelObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        }

        // 그리드
        var gridObj = panelObj.Find("Grid");
        if (gridObj == null)
        {
            GameObject gObj = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj = gObj.transform;
            gridObj.SetParent(panelObj, false);
            gridObj.GetComponent<GridLayoutGroup>().cellSize = new Vector2(100, 100);
            gridObj.GetComponent<GridLayoutGroup>().spacing = new Vector2(10, 10);
            gridObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 500); // 사이즈 대충 잡음
        }

        // 아이콘 프리팹 (임시 버튼)
        string iconPath = "Assets/Resources/BuildingData/BuildingIcon.prefab";
        GameObject iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(iconPath);
        if (iconPrefab == null)
        {
            GameObject btnObj = new GameObject("IconBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnObj.transform, false);
            
            Text t = textObj.GetComponent<Text>();
            t.text = "Item";
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            PrefabUtility.SaveAsPrefabAsset(btnObj, iconPath);
            DestroyImmediate(btnObj);
            iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(iconPath);
        }

        // 4. 컴포넌트 연결
        var uiScript = panelObj.GetComponent<BuildingUI>();
        if (uiScript == null) uiScript = panelObj.gameObject.AddComponent<BuildingUI>();
        
        uiScript.uiPanel = panelObj.gameObject;
        uiScript.gridParent = gridObj;
        uiScript.iconPrefab = iconPrefab;
        
        // 테스트 데이터 주입
        if (uiScript.testPieces == null) uiScript.testPieces = new List<BuildingDataSO>();
        if (!uiScript.testPieces.Contains(floorData))
        {
            uiScript.testPieces.Add(floorData);
        }

        manager.buildingUI = uiScript;

        Debug.Log("✅ [Antigravity] Building System Setup Complete! (Manager + Data + UI)");
    }
}
#endif
