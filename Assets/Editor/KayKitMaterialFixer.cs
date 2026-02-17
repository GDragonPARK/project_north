using UnityEngine;
using UnityEditor;
using System.IO;

public class KayKitMaterialFixer : EditorWindow
{
    [MenuItem("Antigravity/🛠️ Fix KayKit Materials")]
    public static void FixMaterials()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/KayKit" });
        int count = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string matName = Path.GetFileNameWithoutExtension(path).ToLower();
            
            // Expected texture path based on folder structure
            // Example: knight.mat -> Textures/Knight/knight_texture.png
            string folderName = char.ToUpper(matName[0]) + matName.Substring(1);
            string texPath = $"Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Textures/{folderName}/{matName}_texture.png";
            
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                // Try alternate B since A is often used
                texPath = $"Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Textures/{folderName}/{matName}_texture_alt_B.png";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex); // Compatibility
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_Color", Color.white);
                EditorUtility.SetDirty(mat);
                count++;
            }
            else
            {
                Debug.LogWarning($"Texture not found for material: {path} (checked {texPath})");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Fixed {count} KayKit materials with textures.");
    }
}
