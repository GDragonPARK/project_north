using UnityEngine;
using UnityEditor;

public class MaterialURPConverter : EditorWindow
{
    [MenuItem("Antigravity/🛠️ Convert Materials to URP")]
    public static void ConvertStandardToURP()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && (mat.shader.name == "Standard" || mat.shader.name == "Standard (Specular setup)"))
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Converted {count} materials from Standard to URP/Lit.");
    }
}
