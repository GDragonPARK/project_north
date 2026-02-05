using UnityEngine;
using UnityEditor;

public class AvatarMuscleFixerV2 : EditorWindow
{
    [MenuItem("Tools/Fix Avatar Muscles V2")]
    public static void Fix()
    {
        // Target specific model
        string modelPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Characters/Knight.fbx";
        
        AssetImporter importer = AssetImporter.GetAtPath(modelPath);
        ModelImporter modelImporter = importer as ModelImporter;
        
        if(modelImporter)
        {
            SerializedObject so = new SerializedObject(modelImporter);
            HumanDescription hd = modelImporter.humanDescription;
            
            // User requested "Arm Spread" (Arms away from body).
            hd.armStretch = 0.15f; 
            hd.upperArmTwist = 0.5f; 
            
            // Saving
            modelImporter.humanDescription = hd;
            modelImporter.SaveAndReimport();
            Debug.Log($"[MuscleFixer] Updated {modelPath}: ArmStretch=0.15, Twists=0.5");
        }
        else
        {
            Debug.LogError($"[MuscleFixer] Could not find ModelImporter at {modelPath}");
        }
    }
}