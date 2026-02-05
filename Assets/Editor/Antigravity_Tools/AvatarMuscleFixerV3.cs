using UnityEngine;
using UnityEditor;

public class AvatarMuscleFixerV3 : EditorWindow
{
    [MenuItem("Tools/Fix Avatar Muscles V3")]
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
            
            // Increasing ArmStretch to 0.4 (Significant)
            hd.armStretch = 0.4f; 
            hd.upperArmTwist = 0.5f; 
            hd.lowerArmTwist = 0.5f;
            
            // Saving
            modelImporter.humanDescription = hd;
            modelImporter.SaveAndReimport();
            Debug.Log($"[MuscleFixerV3] Updated {modelPath}: ArmStretch=0.4");
        }
    }
}