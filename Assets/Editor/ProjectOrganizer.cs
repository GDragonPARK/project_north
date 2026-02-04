using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProjectOrganizer : EditorWindow
{
    [MenuItem("Tools/Organize Project Structure")]
    public static void OrganizeProject()
    {
        // 1. Create Base Directories
        string[] baseFolders = {
            "Assets/1.Scene",
            "Assets/2.Model",
            "Assets/3.Script",
            "Assets/3.Script/Player",
            "Assets/3.Script/Terrain",
            "Assets/3.Script/UI",
            "Assets/3.Script/System",
            "Assets/4.Sprite",
            "Assets/5.Animation",
            "Assets/6.Materials",
            "Assets/7.PhysicMaterials",
            "Assets/8.Audio",
            "Assets/9.Font",
            "Assets/10.Input",
            "Assets/99.ThirdParty"
        };

        foreach (string folder in baseFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                string name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        // 2. Organization Rules (Source -> Destination)
        // Using partial matches or exact names
        
        // --- Scenes ---
        MoveFile("Assets/Scenes/SampleScene.unity", "Assets/1.Scene/SampleScene.unity");
        MoveFile("Assets/valheim_Data/Scenes/SampleScene.unity", "Assets/1.Scene/SampleScene.unity"); // Check alternative location

        // --- Scripts ---
        // Player
        string[] playerScripts = { "MyPlayerController.cs", "PlayerInteraction.cs", "CharacterStats.cs", "PlayerSpawner.cs", "MinimapCameraFollow.cs" };
        MoveScripts(playerScripts, "Assets/3.Script/Player");

        // Terrain
        string[] terrainScripts = { "TerrainGenerator.cs", "VegetationSpawner.cs", "GhibliWind.cs", "ForceTerrainParams.cs", "New Terrain.asset", "New Terrain 1.asset" }; // Moving terrain assets to terrain script folder or model? Let's put scripts in script. Assets should go to 2.Model or stays? User said "Terrain Generator related". Keeping scripts here.
        MoveScripts(terrainScripts, "Assets/3.Script/Terrain");

        // UI
        string[] uiScripts = { "BossUI.cs", "BuildingMenuUI.cs", "CraftingUI.cs", "FoodBuffUI.cs", "InventoryUI.cs", "StaminaUI.cs", "StorageUI.cs", "TooltipUI.cs", "TooltipTrigger.cs", "MinimapMarker.cs" };
        MoveScripts(uiScripts, "Assets/3.Script/UI");

        // System
        string[] systemScripts = { "BuildManager.cs", "BuildingManager.cs", "CraftingManager.cs", "Equipment_Manager.cs", "FoodSystem.cs", "InventoryManager.cs", "InventorySystem.cs", "SaveManager.cs", "TimeManager.cs", "WeatherManager.cs", "StructureStability.cs", "BossAI.cs", "EnemyAI.cs", "Workbench.cs", "Fireplace.cs", "ConstructionGhost.cs", "BossSummonAltar.cs" };
        MoveScripts(systemScripts, "Assets/3.Script/System");
        
        // Move remaining scripts from valheim_Data/Scripts to 3.Script root
        MoveAllFiles("Assets/valheim_Data/Scripts", "Assets/3.Script", "*.cs");


        // --- Models & Prefabs ---
        // Move KayKit and other models from valheim_Data
        MoveFolder("Assets/valheim_Data/Prefabs", "Assets/2.Model/Prefabs");
        MoveFolder("Assets/valheim_Data/Models", "Assets/2.Model/Models");
        MoveFile("Assets/valheim_Data/GameElements/Spawn_Point.prefab", "Assets/2.Model/Spawn_Point.prefab");

        // --- Third Party ---
        string[] thirdPartyFolders = {
            "Assets/StarterAssets",
            "Assets/3D set of stylized nature - GHIBLI style",
            "Assets/Artsystack - Fantasy RPG GUI",
            "Assets/Fantasy UI SFX - Lite Edition",
            "Assets/Pure Poly",
            "Assets/MapAndRadarSystem",
            "Assets/TextMesh Pro",
            "Assets/com.IvanMurzak",
            "Assets/Plugins" // User asked to move Plugins or create 99.ThirdParty. Moving Plugins INSIDE ThirdParty might break special Unity folder rules. Plugins folder should usually be at root. 
            // User said: "Plugins 또는 99.ThirdParty 폴더를 만들어 그 안으로 한데 모아줘."
            // If I move Plugins to 99.ThirdParty/Plugins, it works but standard practice is root. 
            // However, user explicitly requested "clean up". I will move safe assets to 99.ThirdParty. 
            // "Plugins" folder itself I will check content. If it's just DLLs from assets, maybe okay. 
            // BUT Unity treats "Assets/Plugins" specially (compiles first). Moving it might caus compilation issues.
            // I will move the OTHER folders to 99.ThirdParty. I will leave Plugins at root or move to 99.ThirdParty if safe? 
            // User said "Cleanly gather into Plugins OR 99.ThirdParty". I will choose 99.ThirdParty for assets, and keep Plugins at root if it exists, or verify content.
            // Actually, for safer side, I'll move the non-special folders to 99.ThirdParty.
        };

        foreach (var folder in thirdPartyFolders)
        {
            if (folder == "Assets/Plugins") continue; // Skip Plugins to avoid compilation order issues unless user insists. User gave options.
            MoveFolder(folder, "Assets/99.ThirdParty/" + Path.GetFileName(folder));
        }
        
        // --- Audio & Font ---
        // Move from valheim_Data/Audio or similar
        MoveAllFiles("Assets/valheim_Data/Audio", "Assets/8.Audio", "*");
        MoveAllFiles("Assets/valheim_Data/Fonts", "Assets/9.Font", "*");
        
        // --- Cleanup ---
        AssetDatabase.Refresh();
        Debug.Log("Project Organization Complete.");
    }

    static void MoveFile(string source, string dest)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(source) != null)
        {
            string result = AssetDatabase.MoveAsset(source, dest);
            if (!string.IsNullOrEmpty(result)) Debug.LogError($"Error moving {source}: {result}");
            else Debug.Log($"Moved {source} to {dest}");
        }
    }

    static void MoveScripts(string[] files, string destFolder)
    {
        // Try finding in common locations
        string[] searchPaths = { "Assets/valheim_Data/Scripts", "Assets/Scripts", "Assets/3.Script" };
        
        foreach (var file in files)
        {
            bool found = false;
            foreach (var path in searchPaths)
            {
                string source = $"{path}/{file}";
                if (AssetDatabase.LoadAssetAtPath<Object>(source) != null)
                {
                    MoveFile(source, $"{destFolder}/{file}");
                    found = true;
                    break;
                }
            }
            if (!found) Debug.LogWarning($"Could not find script {file} to move.");
        }
    }

    static void MoveFolder(string source, string dest)
    {
        if (AssetDatabase.IsValidFolder(source))
        {
            string result = AssetDatabase.MoveAsset(source, dest);
            if (!string.IsNullOrEmpty(result)) Debug.LogError($"Error moving {source}: {result}");
            else Debug.Log($"Moved folder {source} to {dest}");
        }
    }

    static void MoveAllFiles(string sourceFolder, string destFolder, string filter)
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder)) return;
        
        string[] files = Directory.GetFiles(sourceFolder, filter);
        foreach (var file in files)
        {
            if (file.EndsWith(".meta")) continue;
            string fileName = Path.GetFileName(file);
            MoveFile(file.Replace("\\", "/"), $"{destFolder}/{fileName}");
        }
    }
}