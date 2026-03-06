using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

[InitializeOnLoad]
public class UltimateCoreSetup
{
    [MenuItem("Antigravity/Force Final Setup")]
    public static void ManualRun()
    {
        RunUltimateSetup();
    }

    public static void RunUltimateSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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
                Image stamFill = allImages.FirstOrDefault(img => img.name == "StaminaBar_Fill");
                if (stamFill != null) {
                    stats.staminaBar = stamFill;
                    changed = true;
                }
            }
            if (stats.healthBar == null) {
                Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Image hpFill = allImages.FirstOrDefault(img => img.name == "HealthBar_Fill");
                if (hpFill != null) {
                    stats.healthBar = hpFill;
                    changed = true;
                }
            }
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
