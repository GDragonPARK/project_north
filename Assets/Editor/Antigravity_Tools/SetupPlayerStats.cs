using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupPlayerStats : EditorWindow
{
    [MenuItem("Tools/Valheim/Hotfix/Link Stamina & Setup Equips")]
    public static void RunHotfix()
    {
        bool changed = false;

        // 1. Player_New 스탯 연결!
        GameObject player = GameObject.Find("Player_New");
        if (player != null)
        {
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = player.AddComponent<CharacterStats>();
                changed = true;
                Debug.Log($"[Hotfix] Attached CharacterStats to {player.name}");
            }

            // UI 찾기
            if (stats.staminaBar == null)
            {
                GameObject stamObj = GameObject.Find("StaminaBar_Fill");
                if (stamObj != null)
                {
                    stats.staminaBar = stamObj.GetComponent<Image>();
                    changed = true;
                    Debug.Log($"[Hotfix] Linked StaminaBar_Fill UI to CharacterStats");
                }
            }
            if (stats.healthBar == null)
            {
                GameObject hpObj = GameObject.Find("HealthBar_Fill");
                if (hpObj != null)
                {
                    stats.healthBar = hpObj.GetComponent<Image>();
                    changed = true;
                    Debug.Log($"[Hotfix] Linked HealthBar_Fill UI to CharacterStats");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Hotfix] Could not find 'Player_New' in the scene.");
        }

        // 2. 횃불(Torch) 프리팹 생성
        string prefabPath = "Assets/Prefabs/Items/Torch.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Items"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
            }

            // 막대기 기반 객체
            GameObject torchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchObj.name = "Torch";
            torchObj.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f); // 얇은 막대
            DestroyImmediate(torchObj.GetComponent<Collider>()); // 콜라이더 불필요

            // 갈색(나무) 머티리얼 적용 노력
            Renderer rend = torchObj.GetComponent<Renderer>();
            Material brownMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            brownMat.color = new Color(0.4f, 0.2f, 0.1f);
            rend.sharedMaterial = brownMat;

            // 불꽃 효과 (Fireplace 복사)
            GameObject fireplace = GameObject.Find("Fireplace");
            if (fireplace != null)
            {
                ParticleSystem ps = fireplace.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    GameObject flameInfo = Instantiate(ps.gameObject, torchObj.transform);
                    flameInfo.name = "FlameParticle";
                    flameInfo.transform.localPosition = new Vector3(0, 1f, 0); // 막대기 끝
                    flameInfo.transform.localScale = Vector3.one * 0.2f;
                }
            }

            // 불빛
            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(torchObj.transform);
            lightObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            Light lgt = lightObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1f, 0.6f, 0.2f);
            lgt.range = 10f;
            lgt.intensity = 2f;

            PrefabUtility.SaveAsPrefabAsset(torchObj, prefabPath);
            DestroyImmediate(torchObj); // 씬에서 삭제
            Debug.Log($"[Hotfix] Created Torch prefab at {prefabPath}");
        }

        // 3. 퀵슬롯 아이콘 세팅 (1:도끼, 2:망치, 3:횃불)
        string[] spriteNames = { "axe", "hammer", "fire" }; // 검색어
        Color[] fallbackColors = { Color.gray, new Color(0.8f, 0.5f, 0.3f), new Color(1f, 0.4f, 0.1f) };

        for (int i = 1; i <= 3; i++)
        {
            GameObject slotObj = GameObject.Find($"QuickSlot_{i}");
            if (slotObj == null) continue;

            Transform iconTr = slotObj.transform.Find("Icon");
            Image iconImg = null;

            if (iconTr == null)
            {
                GameObject newIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                newIcon.transform.SetParent(slotObj.transform, false);
                iconImg = newIcon.GetComponent<Image>();
                
                // 프레임 안쪽에 들어가도록 설정 (여백)
                RectTransform rt = newIcon.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 10);
                rt.offsetMax = new Vector2(-10, -10);
                
                changed = true;
            }
            else
            {
                iconImg = iconTr.GetComponent<Image>();
            }

            if (iconImg != null)
            {
                int index = i - 1;
                // 스프라이트 검색 시도
                string[] guids = AssetDatabase.FindAssets($"{spriteNames[index]} t:Sprite");
                if (guids.Length > 0)
                {
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    iconImg.sprite = s;
                    iconImg.color = Color.white;
                    Debug.Log($"[Hotfix] Assigned {s.name} to QuickSlot_{i}");
                }
                else
                {
                    // 스프라이트가 없으면 임시 색상이라도 넣어둔다.
                    iconImg.sprite = null;
                    iconImg.color = fallbackColors[index];
                    Debug.Log($"[Hotfix] No sprite found for {spriteNames[index]}. Used fallback color for QuickSlot_{i}");
                }
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Hotfix Complete]</color> Stamina linked, Torch created, and QuickSlot icons setup.");
        }
        else
        {
            Debug.Log("[Hotfix] No changes needed or targets not found.");
        }
    }
}
