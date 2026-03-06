using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [Phase 8.1-3] engineer_Wrench 프리팹을 Player_New의 handslot.r에 장착하고
/// EquipmentManager의 hammerObject에 자동 할당한다.
/// </summary>
public static class EquipWrench
{
    private const string WRENCH_PREFAB_PATH = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/engineer_Wrench.prefab";
    private const string HANDSLOT_PATH      = "Rig_Large/root/hips/spine/chest/upperarm.r/lowerarm.r/wrist.r/hand.r/handslot.r";

    [MenuItem("Tools/Valheim/Phase 8.1-3 Equip Wrench")]
    public static void Run()
    {
        // 1. Player_New 탐색
        var player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("[EquipWrench] 씬에서 Player_New를 찾을 수 없습니다.");
            return;
        }

        // 2. handslot.r 뼈대 탐색
        Transform handslot = player.transform.Find(HANDSLOT_PATH);
        if (handslot == null)
        {
            Debug.LogError("[EquipWrench] handslot.r 뼈대를 찾을 수 없습니다: " + HANDSLOT_PATH);
            return;
        }

        // 3. 이미 Wrench가 있으면 재사용, 없으면 생성
        Transform existingWrench = null;
        foreach (Transform child in handslot)
        {
            if (child.name.ToLower().Contains("wrench"))
            {
                existingWrench = child;
                break;
            }
        }

        GameObject wrenchGO;
        if (existingWrench != null)
        {
            wrenchGO = existingWrench.gameObject;
            Debug.Log("[EquipWrench] 기존 Wrench 오브젝트 재사용: " + wrenchGO.name);
        }
        else
        {
            // 4. Wrench 프리팹 로드 및 Instantiate
            var wrenchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WRENCH_PREFAB_PATH);
            if (wrenchPrefab == null)
            {
                Debug.LogError("[EquipWrench] Wrench 프리팹을 찾을 수 없습니다: " + WRENCH_PREFAB_PATH);
                return;
            }

            wrenchGO = (GameObject)PrefabUtility.InstantiatePrefab(wrenchPrefab, handslot);
            wrenchGO.transform.localPosition = Vector3.zero;
            wrenchGO.transform.localRotation = Quaternion.identity;
            wrenchGO.transform.localScale    = Vector3.one;
            wrenchGO.SetActive(false); // 기본 비활성 (맨손 상태)
            Debug.Log("[EquipWrench] ✅ Wrench 장착 완료: " + wrenchGO.name);
        }

        // 5. EquipmentManager 가져오기 (없으면 부착)
        var em = player.GetComponent<EquipmentManager>();
        if (em == null)
        {
            em = player.AddComponent<EquipmentManager>();
            Debug.Log("[EquipWrench] EquipmentManager 새로 추가.");
        }

        // 6. Axe 오브젝트도 재확인하여 할당
        foreach (Transform child in handslot)
        {
            if (child.name.ToLower().Contains("axe"))
            {
                em.axeObject = child.gameObject;
                Debug.Log("[EquipWrench] ✅ axeObject → " + child.name);
                break;
            }
        }

        // 7. Wrench → hammerObject 할당
        em.hammerObject = wrenchGO;
        Debug.Log("[EquipWrench] ✅ hammerObject → " + wrenchGO.name);

        // 8. 씬 저장
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[EquipWrench] ✅ Player_New의 도끼와 망치(렌치)가 EquipmentManager에 완벽하게 자동 연결되었습니다.");
    }
}
