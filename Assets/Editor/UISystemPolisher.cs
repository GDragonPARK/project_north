using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Antigravity.Editor
{
    public class UISystemPolisher : EditorWindow
    {


        private static void PolishInventory()
        {
            InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogError("InventoryUI not found in scene!");
                return;
            }

            if (inventoryUI.m_slotPrefab != null)
            {
                // We should modify the prefab asset if possible, or the scene instance if it's just a scene object.
                // If it is a prefab, let's load it and modify.
                string assetPath = AssetDatabase.GetAssetPath(inventoryUI.m_slotPrefab);
                GameObject root = inventoryUI.m_slotPrefab;
                bool isPrefab = !string.IsNullOrEmpty(assetPath);

                GameObject editable = root;
                if (isPrefab)
                {
                    editable = PrefabUtility.LoadPrefabContents(assetPath);
                }

                // Find Image component to replace sprite
                Image img = editable.GetComponent<Image>();
                if (img)
                {
                    // Look for a nice slot sprite
                    // Updated path based on previous list_dir: Artsystack - Fantasy RPG GUI\ResourcesData\Sprites\components
                    string[] guids = AssetDatabase.FindAssets("ingame_icon_slot t:Sprite"); 
                    // "frame_01" is a guess, let's look for "slot" or "box"
                    if (guids.Length == 0) guids = AssetDatabase.FindAssets("crafting_slot_01 t:Sprite");
                    if (guids.Length == 0) guids = AssetDatabase.FindAssets("item_slot t:Sprite");
                    
                    if (guids.Length > 0)
                    {
                         string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                         Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                         if (newSprite)
                         {
                             img.sprite = newSprite;
                             Debug.Log($"Updated Inventory Slot sprite to: {newSprite.name}");
                         }
                    }
                    else
                    {
                        Debug.LogWarning("Could not find a suitable 'frame' or 'slot' sprite in project.");
                    }
                }

                if (isPrefab)
                {
                    PrefabUtility.SaveAsPrefabAsset(editable, assetPath);
                    PrefabUtility.UnloadPrefabContents(editable);
                }
            }
        }

        private static void PolishMinimap()
        {
            // Find script by type namespace is MapAndRadarSystem
            // We need to use reflection or string search if we don't have the using
            MonoBehaviour minimapScript = null;
            var allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach(var s in allScripts)
            {
                if (s.GetType().FullName.Contains("Minimap"))
                {
                    minimapScript = s;
                    break;
                }
            }

            if (minimapScript != null)
            {
                // Verify it's UI
                RectTransform rect = minimapScript.GetComponent<RectTransform>();
                
                // If script is on a child, find the root UI
                if (rect == null)
                {
                     // Maybe the script isn't on the UI?
                     // Let's assume there is a Canvas for the Minimap or it's part of HUD
                     GameObject minimapUI = GameObject.Find("MinimapUI");
                     if (minimapUI) rect = minimapUI.GetComponent<RectTransform>();
                     else
                     {
                         // Try searching by name "Minimap"
                         GameObject go = GameObject.Find("Minimap");
                         if (go) rect = go.GetComponent<RectTransform>();
                     }
                }

                if (rect != null)
                {
                    // Anchor Top Right
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    rect.anchoredPosition = new Vector2(-20, -20);
                    
                    // FOG OF WAR CONNECTION
                    // Find RawImage for Fog
                    RawImage fogImg = rect.GetComponentInChildren<RawImage>();
                    if (fogImg && fogImg.name.Contains("Fog"))
                    {
                        // Located, check connection
                        FogOfWar fow = Object.FindFirstObjectByType<FogOfWar>();
                        if (fow && fow.fogRenderTexture != null)
                        {
                            fogImg.texture = fow.fogRenderTexture;
                            fogImg.color = new Color(0,0,0, 0.8f); // Darken for fog
                            Debug.Log("Connected FogOfWar Texture to Minimap UI.");
                        }
                    }

                    Debug.Log($"Minimap anchored to Top-Right on {rect.gameObject.name}");
                }
            }
        }

        [MenuItem("Tools/Polish UI System")]
        public static void PolishUI()
        {
            PolishInventory();
            PolishMinimap();
            PolishBars();
        }

        private static void PolishBars()
        {
            // 1. Stamina UI
            StaminaUI staminaUI = Object.FindFirstObjectByType<StaminaUI>();
            if (staminaUI)
            {
               Slider slider = staminaUI.GetComponentInChildren<Slider>();
               if (slider)
               {
                   ApplyBarStyle(slider, "progress_bar_bg", "vs_progress_yellow"); 
               }
            }

            // 2. Health UI (Assuming Standard Name or Component)
            // Often just a Slider on a Canvas named "HealthBar"
            GameObject healthGo = GameObject.Find("HealthBar");
            if (healthGo)
            {
                Slider slider = healthGo.GetComponent<Slider>();
                if (slider) ApplyBarStyle(slider, "progress_bar_bg", "vs_progress_green");
            }
        }

        private static void ApplyBarStyle(Slider slider, string bgName, string fillName)
        {
            if (!slider) return;

            // Background
            Image bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg)
            {
                 string[] guids = AssetDatabase.FindAssets($"{bgName} t:Sprite");
                 if (guids.Length > 0) bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // Fill
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill)
            {
                 string[] guids = AssetDatabase.FindAssets($"{fillName} t:Sprite");
                 if (guids.Length > 0) fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            Debug.Log($"Polished Bar: {slider.name}");
        }
    }
}
