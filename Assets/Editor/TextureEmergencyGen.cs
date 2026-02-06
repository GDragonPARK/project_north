using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureEmergencyGen : EditorWindow
{
    [MenuItem("Antigravity/Emergency Fix: Generate & Apply Terrain Textures")]
    public static void GenerateAndApply()
    {
        // 1. Generate Textures in Memory
        Texture2D rock = MakeTexture(256, 256, new Color(0.4f, 0.4f, 0.4f)); // Gray
        Texture2D grass = MakeTexture(256, 256, new Color(0.1f, 0.6f, 0.1f)); // Green

        // 2. Save to Assets
        string rockPath = "Assets/Textures/Gen_Rock.png";
        string grassPath = "Assets/Textures/Gen_Grass.png";
        
        EnsureDirectory("Assets/Textures");
        
        File.WriteAllBytes(rockPath, rock.EncodeToPNG());
        File.WriteAllBytes(grassPath, grass.EncodeToPNG());
        
        AssetDatabase.Refresh(); // Import them

        // 3. Load Imported Assets
        Texture2D rockAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(rockPath);
        Texture2D grassAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(grassPath);

        // 4. Assign to TerrainGenerator
        TerrainGenerator tg = Object.FindObjectOfType<TerrainGenerator>();
        if (!tg)
        {
            // If TG is missing, try finding Terrain and adding it?
            Terrain t = Object.FindObjectOfType<Terrain>();
            if (t) 
            {
                 tg = t.gameObject.GetComponent<TerrainGenerator>();
                 if (!tg) tg = t.gameObject.AddComponent<TerrainGenerator>();
            }
            else
            {
                Debug.LogError("No Terrain found to fix!");
                return;
            }
        }

        tg.rockTexture = rockAsset;
        tg.grassTexture = grassAsset;
        
        Debug.Log($"Assigned Generated Textures: {rockAsset.name}, {grassAsset.name}");

        // 5. Force Re-Generate
        tg.GenerateTerrain();
        Debug.Log("Forced Terrain Re-Generation with Valid Textures.");
    }

    private static Texture2D MakeTexture(int w, int h, Color col)
    {
        Texture2D tex = new Texture2D(w, h);
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
