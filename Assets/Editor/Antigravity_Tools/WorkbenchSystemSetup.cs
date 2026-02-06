using UnityEngine;
using UnityEditor;
using TMPro;

public class WorkbenchSystemSetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Workbench System")]
    public static void Setup()
    {
        // 1. Create Workbench Prefab
        string prefabPath = "Assets/Prefabs/Workbench.prefab";
        GameObject wb = null;
        
        // Check if exists
        wb = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (wb == null)
        {
            // Create Prototype
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Workbench";
            go.transform.localScale = new Vector3(2, 1, 1);
            go.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.6f, 0.3f, 0f) }; // Brown
            
            go.AddComponent<Workbench>();
            // Collider already exists (BoxCollider from Cube)
            
            if (!System.IO.Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            wb = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("Created Workbench Prefab.");
        }

        // 2. Assign to BuildingManager
        BuildingManager bm = Object.FindFirstObjectByType<BuildingManager>();
        if (bm)
        {
            Undo.RecordObject(bm, "Assign Workbench");
            bm.workbenchPrefab = wb;
            EditorUtility.SetDirty(bm);
        }
        else
        {
            Debug.LogError("BuildingManager not found!");
        }

        // 3. Create Warning Text
        SetupWarningUI(bm);
    }

    private static void SetupWarningUI(BuildingManager bm)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas) 
        {
            Debug.LogWarning("No Canvas found for Warning UI.");
            return;
        }

        TextMeshProUGUI txt = null;
        Transform t = canvas.transform.Find("WarningText");
        if (t)
        {
            txt = t.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject go = new GameObject("WarningText");
            go.transform.SetParent(canvas.transform, false);
            txt = go.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 36;
            txt.color = Color.red;
            txt.text = ""; // Empty by default
            
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 100); // Center Screen, slightly up
            rt.sizeDelta = new Vector2(500, 100);
            
            Undo.RegisterCreatedObjectUndo(go, "Create WarningText");
        }

        if (bm && txt)
        {
            bm.warningText = txt;
            EditorUtility.SetDirty(bm);
        }
    }
}
