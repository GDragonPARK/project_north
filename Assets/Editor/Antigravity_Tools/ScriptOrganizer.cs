using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Antigravity.Tools
{
    [InitializeOnLoad]
    public class ScriptOrganizer
    {
        static ScriptOrganizer()
        {
            Debug.Log("Antigravity ScriptOrganizer Loaded and Ready.");
        }

        [MenuItem("Antigravity/Organize Scripts")]
        public static void OrganizeScripts()
        {
            Debug.Log("Starting Script Reorganization...");

            // Define Mappings
            Dictionary<string, string> mappings = new Dictionary<string, string>()
            {
                // Build
                { "BuildingManager.cs", "Build" },
                { "BuildSystem.cs", "Build" },
                { "ConstructionGhost.cs", "Build" },
                { "SnapPoint.cs", "Build" },
                { "StructureStability.cs", "Build" },
                { "Workbench.cs", "Build" },
                { "Fireplace.cs", "Build" },
                { "PlacementController.cs", "Build" },

                // System
                { "TimeManager.cs", "System" },
                { "WeatherManager.cs", "System" },
                { "SaveManager.cs", "System" },
                { "BossAI.cs", "System" },
                { "BossSummonAltar.cs", "System" },
                { "EnemyAI.cs", "System" },
                { "FoodSystem.cs", "System" },
                { "FogOfWar.cs", "System" },
                { "CameraInputBridge.cs", "System" },
                { "CameraZoom.cs", "System" },
                
                // Core
                { "HealthSystem.cs", "Core" },
                { "ObjectPoolManager.cs", "Core" },
                { "PooledParticle.cs", "Core" },
                { "StartupFloor.cs", "Core" },

                // Player
                { "CharacterStats.cs", "Player" },
                { "PlayerEquipmentManager.cs", "Player" },
                { "MyPlayerController.cs", "Player" },
                { "AxeAdjuster.cs", "Player" },
                { "PlayerAnimationEvents.cs", "Player" },
                { "PlayerHarvestingIK.cs", "Player" },
                { "PlayerInteraction.cs", "Player" },
                { "PlayerSpawner.cs", "Player" },
                { "WeaponDamageController.cs", "Player" },
                { "MinimapCameraFollow.cs", "Player" },

                // Resource
                { "ResourceNode.cs", "Resource" },
                { "FallenLog.cs", "Resource" },
                { "PickupItem.cs", "Resource" },
                { "FallenTreeLoot.cs", "Resource" },
                { "ResourceObject.cs", "Resource" },

                // Terrain
                { "TerrainGenerator.cs", "Terrain" },
                { "VegetationSpawner.cs", "Terrain" },
                { "GhibliWind.cs", "Terrain" },

                // UI
                { "InventoryManager.cs", "UI" },
                { "SlotUI.cs", "UI" },
                { "EquipmentUI.cs", "UI" },
                { "StorageContainer.cs", "UI" },
                { "InventorySystem.cs", "UI" },
                { "Equipment_Manager.cs", "UI" },
                { "CraftingManager.cs", "UI" },

                // UI/Data
                { "ItemData.cs", "UI/Data" },
                { "RecipeData.cs", "UI/Data" },
                { "CraftingRecipe.cs", "UI/Data" },
                { "InventoryItem.cs", "UI/Data" },
            };

            string rootPath = "Assets/3.Script";
            if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "3.Script");

            // Find all .cs files in assets
            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            int movedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileName(path);

                if (path.Contains("Assets/99.ThirdParty") || path.Contains("Assets/StarterAssets") || path.Contains("/Editor/"))
                    continue;

                if (mappings.ContainsKey(filename))
                {
                    string targetSubFolder = mappings[filename]; // e.g., "UI/Data"
                    string fullTargetFolderPath = rootPath;

                    // Handle nested folders like UI/Data
                    string[] folders = targetSubFolder.Split('/');
                    foreach(string folder in folders)
                    {
                        string nextPath = Path.Combine(fullTargetFolderPath, folder).Replace("\\", "/");
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(fullTargetFolderPath, folder);
                        }
                        fullTargetFolderPath = nextPath;
                    }

                    string targetPath = Path.Combine(fullTargetFolderPath, filename).Replace("\\", "/");

                    if (path != targetPath)
                    {
                        string error = AssetDatabase.MoveAsset(path, targetPath);
                        if (string.IsNullOrEmpty(error))
                        {
                            // Debug.Log($"Moved {filename} -> {targetSubFolder}"); // Reduced spam
                            movedCount++;
                        }
                        else
                        {
                            Debug.LogError($"Failed to move {filename}: {error}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reorganization Complete. Moved {movedCount} scripts.");
        }
    }
}
