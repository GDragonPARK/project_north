using UnityEngine;
using UnityEditor;

public class WoodProceduralGenerator : EditorWindow
{
    [MenuItem("Antigravity/🪵 Generate Procedural Wood")]
    public static void Generate()
    {
        string prefabPath = "Assets/Prefabs/Wood.prefab";
        
        // 1. Create Root
        GameObject root = new GameObject("Wood");
        root.layer = 10; // Item Layer

        // 2. Material Setup
        Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        string texPath = "Assets/99.ThirdParty/3D set of stylized nature - GHIBLI style/Art/Textures/Wood_1_Base_Color.png";
        Texture2D woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (woodTex) woodMat.mainTexture = woodTex;
        woodMat.SetFloat("_Smoothness", 0.0f);
        
        string matPath = "Assets/Materials/GeneratedWood.mat";
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(woodMat, matPath);

        // 3. Procedural Logs (Cylinders)
        CreateLog(root, woodMat, new Vector3(0, 0, 0), new Vector3(0, 0, 90), new Vector3(0.15f, 0.4f, 0.15f));
        CreateLog(root, woodMat, new Vector3(0.1f, 0.05f, 0), new Vector3(10, 5, 85), new Vector3(0.14f, 0.4f, 0.14f));
        CreateLog(root, woodMat, new Vector3(-0.08f, 0.08f, 0.05f), new Vector3(-5, 0, 95), new Vector3(0.13f, 0.38f, 0.13f));

        // 4. Sparkle FX (Billboard Sprite)
        GameObject sparkle = new GameObject("InteractionFX");
        sparkle.transform.SetParent(root.transform);
        sparkle.transform.localPosition = new Vector3(0, 0.5f, 0); // Float above
        
        SpriteRenderer sr = sparkle.AddComponent<SpriteRenderer>();
        string glowPath = "Assets/99.ThirdParty/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/Glow.png";
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(glowPath);
        if (glowSprite) sr.sprite = glowSprite;
        else Debug.LogWarning("Glow sprite not found at " + glowPath);
        
        sr.color = new Color(1f, 1f, 0.8f, 0.8f); // Warm yellow
        sparkle.transform.localScale = Vector3.one * 0.5f;
        sparkle.SetActive(false); // Hidden by default

        // Billboard script? Or just face camera in PlayerInteraction? 
        // For simplicity, let's assume PlayerInteraction or a simple script will handle visuals.

        // 5. Physics & Logic
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0, 0);
        col.size = new Vector3(0.5f, 0.3f, 0.8f); // Manual fit for the pile

        ItemObject io = root.AddComponent<ItemObject>();
        io.itemName = "Wood";
        io.amount = 1;

        // Save & Cleanup
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log("<color=green><b>[Antigravity]</b> Procedural Wood Generated Successfully!</color>");
    }

    private static void CreateLog(GameObject parent, Material mat, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        log.transform.SetParent(parent.transform);
        log.transform.localPosition = pos;
        log.transform.localEulerAngles = rot;
        log.transform.localScale = scale;
        
        log.GetComponent<MeshRenderer>().sharedMaterial = mat;
        DestroyImmediate(log.GetComponent<Collider>()); // Remove default CapsuleCollider
    }
}
