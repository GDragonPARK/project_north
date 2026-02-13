using UnityEngine;
using UnityEditor;
using System.IO;

public class TerrainURPFixer : EditorWindow
{
    [MenuItem("Antigravity/FINAL FIX: Force URP Terrain & Textures")]
    public static void ForceFix()
    {
        Debug.Log("Starting Terrain Fix...");

        // 1. Ensure Directories
        string textureDir = "Assets/Textures";
        string layerDir = "Assets/Settings/TerrainLayers"; // User requested specific check for this
        if (!Directory.Exists(textureDir)) Directory.CreateDirectory(textureDir);
        if (!Directory.Exists(layerDir)) Directory.CreateDirectory(layerDir);

        // 2. Generate/Load Textures
        string rockPath = textureDir + "/Gen_Rock.png";
        string grassPath = textureDir + "/Gen_Grass.png";

        Texture2D rockTex = LoadOrGenerateTexture(rockPath, new Color(0.4f, 0.4f, 0.4f));
        Texture2D grassTex = LoadOrGenerateTexture(grassPath, new Color(0.1f, 0.6f, 0.1f));

        // 3. Create Terrain Layers (Persistent Assets)
        TerrainLayer rockLayer = CreateLayer(layerDir + "/Layer_Rock.terrainlayer", rockTex);
        TerrainLayer grassLayer = CreateLayer(layerDir + "/Layer_Grass.terrainlayer", grassTex);

        // 4. Find and Fix Terrain
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        if (terrains.Length == 0)
        {
            Debug.LogError("No Terrain found in scene!");
            return;
        }

        foreach (Terrain t in terrains)
        {
            // Set Material to URP Lit
            Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            if (urpMat.shader == null)
            {
                Debug.LogError("URP Shader not found! Is URP installed? Falling back to Standard.");
                 // Fallback or keep existing if URP missing, but try to set name at least
            }
            else
            {
                t.materialTemplate = urpMat;
                t.materialType = Terrain.MaterialType.Custom; // Often needed for URP
            }

            // Set Layers
            t.terrainData.terrainLayers = new TerrainLayer[] { rockLayer, grassLayer };
            
            Debug.Log($"Fixed Terrain: {t.name} (Shader: {t.materialTemplate.shader.name})");
        }

        // 5. Merge Environment Managers
        GameObject env = GameObject.Find("Environment");
        GameObject envMgr = GameObject.Find("Environment_Manager");

        if (env && envMgr)
        {
            Debug.Log("Merging Environment_Manager into Environment...");
            // Move children from Mgr to Env
            while(envMgr.transform.childCount > 0)
            {
                Transform child = envMgr.transform.GetChild(0);
                child.SetParent(env.transform);
            }
            // Destroy empty manager
            DestroyImmediate(envMgr);
        }
        else
        {
             Debug.Log("No duplicate environment managers found to merge.");
        }
        
        // 6. Refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Terrain Fix Complete! Please check the Scene view.");
    }

    private static Texture2D LoadOrGenerateTexture(string path, Color col)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            tex = new Texture2D(256, 256);
            Color[] pix = new Color[256 * 256];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return tex;
    }

    private static TerrainLayer CreateLayer(string path, Texture2D tex)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer();
            layer.diffuseTexture = tex;
            layer.tileSize = new Vector2(5, 5);
            AssetDatabase.CreateAsset(layer, path);
        }
        else
        {
            // Update existing
            layer.diffuseTexture = tex;
            EditorUtility.SetDirty(layer);
        }
        return layer;
    }
}
