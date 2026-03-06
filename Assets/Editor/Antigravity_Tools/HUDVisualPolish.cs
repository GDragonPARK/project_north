using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class HUDVisualPolish : EditorWindow
{
    [MenuItem("Tools/Valheim/Polish HUD (QuickSlots)")]
    public static void PolishQuickSlots()
    {
        // 1. 씬 내의 모든 Image 컴포넌트를 스캔하여 퀵슬롯 패널 탐색
        Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // 우드 프레임용 스프라이트 로드 (액자 테두리 용도)
        Sprite frameSprite = null;
        string[] frameGuids = AssetDatabase.FindAssets("woodpanel_ t:Sprite");
        if (frameGuids.Length > 0)
        {
            frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(frameGuids[0]));
        }

        int modifiedSlotCount = 0;
        int modifiedTextCount = 0;

        foreach (Image img in allImages)
        {
            string objName = img.gameObject.name.ToLower();
            Transform parentTransform = img.transform.parent;
            string parentName = parentTransform != null ? parentTransform.name.ToLower() : "";

            // 2. [지능적 식별] 이름에 Quick, Hotbar, Slot, Inventory 등이 포함되어 있는지 체크
            // 보통 슬롯의 '배경' 역할을 하는 객체는 맨 뒤에 렌더링되게끔 루트이거나 특정 이름을 가짐
            bool isSlotBackground = objName.Contains("slot") || objName.Contains("bg") || objName.Contains("background");
            bool isInHUDPanel = parentName.Contains("quick") || parentName.Contains("hotbar") || parentName.Contains("inventory");

            // 예: "QuickSlot_1"의 배경 Image 라던가, "SlotBG" 같은 구조
            if ((isSlotBackground && isInHUDPanel) || objName.Contains("quickslot"))
            {
                // [배경 프레임 교체]
                // 밋밋한 박스 지우기 및 밝은 우드톤 덮어씌움 (Phase 11.6 수정)
                img.color = new Color(0.9f, 0.9f, 0.9f, 0.85f); 

                // 액자 느낌의 얇은 우드 프레임 입히기
                if (frameSprite != null)
                {
                    img.sprite = frameSprite;
                    img.type = Image.Type.Sliced;
                }
                
                modifiedSlotCount++;

                // [자식 텍스트 렌더링 가독성 업그레이드]
                // 해당 슬롯 하위에 있는 번호 텍스트(TextMeshProUGUI) 탐색
                TextMeshProUGUI[] childTexts = img.transform.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in childTexts)
                {
                    // 수량(Amount) 텍스트가 아닌 '단축키 번호' 텍스트를 우선 타겟팅
                    // (숫자 1,2,3... 만 있거나, 번호 표시용 텍스트인 경우)
                    if (txt.gameObject.name.ToLower().Contains("num") || txt.text.Length <= 2)
                    {
                        Undo.RecordObject(txt, "Polish QuickSlot Text");

                        // 굵게(Bold) 및 화려한 노란빛/흰색 컬러 부여
                        txt.fontStyle |= FontStyles.Bold;
                        txt.color = new Color32(255, 230, 100, 255); // 밝고 따뜻한 노란빛

                        // 뒤에 아이템 아이콘이 깔려도 선명하도록 그림자(Underlay) 또는 크기 약간 증폭
                        txt.fontSize = Mathf.Max(txt.fontSize, 18f); 

                        EditorUtility.SetDirty(txt);
                        modifiedTextCount++;
                    }
                }

                EditorUtility.SetDirty(img);
            }
        }

        // 3. 더티 마킹 및 로그 출력
        if (modifiedSlotCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=orange>[QuickSlot Polish Success]</color> Applied dark survival frames to {modifiedSlotCount} slots and enhanced {modifiedTextCount} hotkey texts.");
        }
        else
        {
            Debug.LogWarning("[QuickSlot Polish] Could not find any target slots. Make sure objects contain 'Quick', 'Hotbar', or 'Slot' in their hierarchy names.");
        }
    }

    [MenuItem("Tools/Valheim/Ultimate Core Setup")]
    public static void PolishUltimateCore()
    {
        bool changed = false;

        // 1. Stamina Linkage
        GameObject player = GameObject.Find("Player_New");
        if (player != null)
        {
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats == null) {
                stats = player.AddComponent<CharacterStats>();
                changed = true;
            }

            if (stats.staminaBar == null) {
                Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Image stamFill = System.Linq.Enumerable.FirstOrDefault(allImages, img => img.name == "StaminaBar_Fill");
                if (stamFill != null) {
                    stats.staminaBar = stamFill;
                    changed = true;
                }
            }
            if (stats.healthBar == null) {
                Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Image hpFill = System.Linq.Enumerable.FirstOrDefault(allImages, img => img.name == "HealthBar_Fill");
                if (hpFill != null) {
                    stats.healthBar = hpFill;
                    changed = true;
                }
            }

            System.Action<Image> setupFill = (img) => {
                if (img == null) return;
                if (img.sprite == null) {
                    img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("UI/Skin/UISprite.psd") 
                        ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                }
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                changed = true;
                EditorUtility.SetDirty(img);
            };

            setupFill(stats.staminaBar);
            setupFill(stats.healthBar);
        }

        // 2. Torch Prefab
        string prefabPath = "Assets/Prefabs/Items/Torch.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Items"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
            }

            GameObject torchRoot = new GameObject("Torch");
            
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(torchRoot.transform);
            handle.transform.localPosition = Vector3.zero;
            handle.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            Renderer rend = handle.GetComponent<Renderer>();
            Material brownMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (brownMat.shader.isSupported) {
                brownMat.color = new Color(0.4f, 0.2f, 0.1f);
                rend.sharedMaterial = brownMat;
            }

            GameObject fireplace = GameObject.Find("Fireplace");
            GameObject flameRoot = new GameObject("Flame");
            flameRoot.transform.SetParent(torchRoot.transform);
            flameRoot.transform.localPosition = new Vector3(0, 0.4f, 0);

            if (fireplace != null)
            {
                ParticleSystem ps = fireplace.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    GameObject flameInfo = Object.Instantiate(ps.gameObject, flameRoot.transform);
                    flameInfo.name = "FlameParticle";
                    flameInfo.transform.localPosition = Vector3.zero;
                    flameInfo.transform.localScale = Vector3.one * 0.2f;
                }
            }

            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(flameRoot.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light lgt = lightObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1f, 0.6f, 0.2f);
            lgt.range = 10f;
            lgt.intensity = 2f;

            PrefabUtility.SaveAsPrefabAsset(torchRoot, prefabPath);
            Object.DestroyImmediate(torchRoot);
            Debug.Log($"Created Torch prefab at {prefabPath}");
        }

        // 3. QuickSlot Setup
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform targetContainer = null;
        
        foreach(var rawC in canvases)
        {
            Transform[] allChildren = rawC.GetComponentsInChildren<Transform>(true);
            foreach(var t in allChildren)
            {
                string n = t.name.ToLower();
                if ((n.Contains("quick") || n.Contains("hotbar") || n.Contains("inventory")) && n.Contains("slot") == false)
                {
                    int count = 0;
                    foreach(Transform child in t) {
                        if (child.GetComponent<Image>() != null) count++;
                    }
                    if (count >= 3) {
                        targetContainer = t;
                        break;
                    }
                }
            }
            if (targetContainer != null) break;
        }

        // Fallback robust search
        if (targetContainer == null) {
            Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach(var img in allImages) {
                if (img.transform.parent != null && img.transform.parent.childCount >= 3) {
                    string pn = img.transform.parent.name.ToLower();
                    if (pn.Contains("quick") || pn.Contains("hotbar")) {
                        targetContainer = img.transform.parent;
                        break;
                    }
                }
            }
        }

        if (targetContainer != null)
        {
            string[] spriteNames = { "axe", "hammer", "fire" }; 
            Color[] fallbackColors = { Color.gray, new Color(0.8f, 0.5f, 0.3f), new Color(1f, 0.4f, 0.1f) };

            int slotsMapped = 0;
            foreach(Transform child in targetContainer)
            {
                if (slotsMapped >= 3) break;
                
                Image bg = child.GetComponent<Image>();
                if (bg == null) continue;

                Transform iconTr = child.Find("Icon");
                Image iconImg = null;

                if (iconTr == null)
                {
                    GameObject newIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    newIcon.transform.SetParent(child, false);
                    iconImg = newIcon.GetComponent<Image>();
                    
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
                    string[] guids = AssetDatabase.FindAssets($"{spriteNames[slotsMapped]} t:Sprite");
                    if (guids.Length > 0)
                    {
                        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
                        iconImg.sprite = s;
                        iconImg.color = Color.white;
                    }
                    else
                    {
                        iconImg.sprite = null;
                        iconImg.color = fallbackColors[slotsMapped];
                    }
                    changed = true;
                }
                slotsMapped++;
            }
        }
        else
        {
            Debug.LogWarning("[UltimateCoreSetup] Could not find QuickSlot container to setup icons.");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=cyan><b>[UltimateCoreSetup Complete]</b></color> Stamina safely linked, Torch Prefab strictly built, and QuickSlot icons dynamically assigned!");
        }
    }
}
