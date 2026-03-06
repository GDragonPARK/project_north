using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixRoofAndCost : Editor
{
    [MenuItem("Tools/Valheim/Phase 10.6 - Fix Roof And Cost")]
    public static void RunFix()
    {
        FixBuildingCost();
        FixRoofSlopesOffset();
    }

    private static void FixBuildingCost()
    {
        GameObject canvasObj = GameObject.Find("Canvas_Building");
        if (canvasObj == null)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
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
            Debug.LogError("[FixRoofAndCost] 씬에서 'Canvas_Building'을 찾을 수 없습니다.");
            return;
        }

        int modifyCount = 0;
        var buttons = canvasObj.GetComponentsInChildren<BuildUIButton>(true);
        foreach (var btn in buttons)
        {
            if (btn.requiredAmount != 0)
            {
                btn.requiredAmount = 0;
                EditorUtility.SetDirty(btn);
                EditorUtility.SetDirty(btn.gameObject);
                modifyCount++;
            }
        }

        if (modifyCount > 0)
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log($"[FixRoofAndCost] 총 {modifyCount}개의 건축 버튼 자원 소모량을 0으로 변경했습니다 (무한 건축 모드).");
        }
        else
        {
            Debug.Log("[FixRoofAndCost] 이미 모든 건축 버튼의 자원 소모량이 0이거나 버튼을 찾을 수 없습니다.");
        }
    }

    private static void FixRoofSlopesOffset()
    {
        string[] prefabs45 = new string[]
        {
            "Assets/Prefabs/Building/WoodRoof_45_Ghost.prefab",
            "Assets/Prefabs/Building/WoodRoof_45_Real.prefab"
        };
        
        string[] prefabs30 = new string[]
        {
            "Assets/Prefabs/Building/WoodRoof_30_Ghost.prefab",
            "Assets/Prefabs/Building/WoodRoof_30_Real.prefab"
        };

        int count = 0;

        // 45도 지붕 핫픽스 (하드코딩된 오프셋 적용)
        foreach (string path in prefabs45)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root == null) continue;

                Transform visualChild = root.transform.Find("Visuals");
                if (visualChild != null)
                {
                    // 45도 지붕 대략적 오프셋: Y = 1.06f, Z = 1.06f
                    visualChild.localPosition = new Vector3(0f, 1.06f, 1.06f);
                    EditorUtility.SetDirty(visualChild.gameObject);
                    count++;
                }
            }
        }

        // 30도 지붕 핫픽스 (하드코딩된 오프셋 적용)
        foreach (string path in prefabs30)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root == null) continue;

                Transform visualChild = root.transform.Find("Visuals");
                if (visualChild != null)
                {
                    // 30도 지붕 대략적 오프셋: Y = 0.75f, Z = 1.3f
                    visualChild.localPosition = new Vector3(0f, 0.75f, 1.30f);
                    EditorUtility.SetDirty(visualChild.gameObject);
                    count++;
                }
            }
        }

        Debug.Log($"[FixRoofAndCost] 총 {count}개의 지붕 프리팹(Ghost/Real) Visuals 오프셋(피벗) 핫픽스 완료!");
    }
}
