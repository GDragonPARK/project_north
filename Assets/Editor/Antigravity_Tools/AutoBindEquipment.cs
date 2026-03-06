using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [Phase 8.1-2] Player_New의 자식 계층에서 Axe/Hammer 오브젝트를 자동으로 찾아
/// EquipmentManager에 할당하는 에디터 툴.
/// </summary>
public static class AutoBindEquipment
{
    [MenuItem("Tools/Valheim/Auto Bind Equipment")]
    public static void BindTools()
    {
        // 1. Player_New 탐색 (씬)
        var playerGO = GameObject.Find("Player_New");
        if (playerGO == null)
        {
            Debug.LogError("[AutoBindEquipment] 씬에서 Player_New를 찾을 수 없습니다.");
            return;
        }

        // 2. EquipmentManager 가져오기 (없으면 부착)
        var em = playerGO.GetComponent<EquipmentManager>();
        if (em == null)
        {
            em = playerGO.AddComponent<EquipmentManager>();
            Debug.Log("[AutoBindEquipment] EquipmentManager 컴포넌트를 새로 추가했습니다.");
        }

        // 3. 모든 자식 Transform 순회하며 이름 기반 탐색
        var allTransforms = playerGO.GetComponentsInChildren<Transform>(true);

        GameObject foundAxe    = null;
        GameObject foundHammer = null;

        foreach (var t in allTransforms)
        {
            string nameLower = t.gameObject.name.ToLower();

            // Axe 탐색: axe 포함, rig/bone 제외
            if (foundAxe == null &&
                nameLower.Contains("axe") &&
                !nameLower.Contains("bone") &&
                !nameLower.Contains("rig"))
            {
                foundAxe = t.gameObject;
            }

            // Hammer 탐색: hammer 포함, rig/bone 제외
            if (foundHammer == null &&
                nameLower.Contains("hammer") &&
                !nameLower.Contains("bone") &&
                !nameLower.Contains("rig"))
            {
                foundHammer = t.gameObject;
            }

            if (foundAxe != null && foundHammer != null) break;
        }

        // 4. 할당
        bool anyBound = false;

        if (foundAxe != null)
        {
            em.axeObject = foundAxe;
            Debug.Log("[AutoBindEquipment] ✅ axeObject → " + foundAxe.name);
            anyBound = true;
        }
        else
        {
            Debug.LogWarning("[AutoBindEquipment] ⚠️ 'Axe'가 포함된 자식 오브젝트를 찾지 못했습니다.");
        }

        if (foundHammer != null)
        {
            em.hammerObject = foundHammer;
            Debug.Log("[AutoBindEquipment] ✅ hammerObject → " + foundHammer.name);
            anyBound = true;
        }
        else
        {
            Debug.LogWarning("[AutoBindEquipment] ⚠️ 'Hammer'가 포함된 자식 오브젝트를 찾지 못했습니다.");
        }

        // 5. 저장
        EditorUtility.SetDirty(playerGO);
        EditorSceneManager.MarkSceneDirty(playerGO.scene);
        EditorSceneManager.SaveOpenScenes();

        if (anyBound)
            Debug.Log("[AutoBindEquipment] ✅ Player_New의 도끼와 망치가 EquipmentManager에 완벽하게 자동 연결되었습니다.");
        else
            Debug.LogWarning("[AutoBindEquipment] 도끼/망치 오브젝트를 모두 찾지 못했습니다. 오브젝트 이름을 확인해 주세요.");
    }
}
