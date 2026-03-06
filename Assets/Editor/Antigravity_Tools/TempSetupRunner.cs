using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TempSetupRunner : EditorWindow
{
    [MenuItem("Tools/Valheim/Execute Temp Setup")]
    public static void Run()
    {
        bool changed = false;

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

            if (stats.staminaBar == null)
            {
                GameObject stamObj = GameObject.Find("StaminaBar_Fill");
                if (stamObj != null)
                {
                    stats.staminaBar = stamObj.GetComponent<Image>();
                    changed = true;
                }
            }
            if (stats.healthBar == null)
            {
                GameObject hpObj = GameObject.Find("HealthBar_Fill");
                if (hpObj != null)
                {
                    stats.healthBar = hpObj.GetComponent<Image>();
                    changed = true;
                }
            }
        }

        string prefabPath = "Assets/Prefabs/Items/Torch.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Items"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
            }

            GameObject torchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchObj.name = "Torch";
            torchObj.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f); 
            DestroyImmediate(torchObj.GetComponent<Collider>());

            Renderer rend = torchObj.GetComponent<Renderer>();
            Material brownMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            brownMat.color = new Color(0.4f, 0.2f, 0.1f);
            rend.sharedMaterial = brownMat;

            GameObject fireplace = GameObject.Find("Fireplace");
            if (fireplace != null)
            {
                ParticleSystem ps = fireplace.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    GameObject flameInfo = Instantiate(ps.gameObject, torchObj.transform);
                    flameInfo.name = "FlameParticle";
                    flameInfo.transform.localPosition = new Vector3(0, 1f, 0);
                    flameInfo.transform.localScale = Vector3.one * 0.2f;
                }
            }

            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(torchObj.transform);
            lightObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            Light lgt = lightObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1f, 0.6f, 0.2f);
            lgt.range = 10f;
            lgt.intensity = 2f;

            PrefabUtility.SaveAsPrefabAsset(torchObj, prefabPath);
            DestroyImmediate(torchObj);
        }

        string[] spriteNames = { "axe", "hammer", "fire" }; 
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
                string[] guids = AssetDatabase.FindAssets($"{spriteNames[index]} t:Sprite");
                if (guids.Length > 0)
                {
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    iconImg.sprite = s;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.sprite = null;
                    iconImg.color = fallbackColors[index];
                }
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Temp Execute Complete]</color> Stamina linked, Torch created, and QuickSlot icons setup.");
        }
    }
}
