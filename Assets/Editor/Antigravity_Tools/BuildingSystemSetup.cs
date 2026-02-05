using UnityEngine;
using UnityEditor;

public class BuildingSystemSetup : EditorWindow
{
    [MenuItem("Tools/Setup Building System")]
    public static void Setup()
    {
        // 1. Create Materials
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material green = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        green.color = new Color(0, 1, 0, 0.5f);
        green.SetFloat("_Surface", 1); // Transparent
        green.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        green.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        green.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        green.SetInt("_ZWrite", 0);
        green.renderQueue = 3000;
        AssetDatabase.CreateAsset(green, "Assets/Materials/Ghost_Green.mat");

        Material red = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        red.color = new Color(1, 0, 0, 0.5f);
        red.SetFloat("_Surface", 1); // Transparent
        red.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        red.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        red.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        red.SetInt("_ZWrite", 0);
        red.renderQueue = 3000;
        AssetDatabase.CreateAsset(red, "Assets/Materials/Ghost_Red.mat");

        // 2. Create Prefabs
        GameObject wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallObj.name = "WoodWall";
        wallObj.transform.localScale = new Vector3(2f, 2f, 0.2f);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        GameObject wallPrefab = PrefabUtility.SaveAsPrefabAsset(wallObj, "Assets/Prefabs/WoodWall.prefab");
        
        // Ghost Prefab
        GameObject ghostObj = GameObject.Instantiate(wallObj);
        ghostObj.name = "WoodWall_Ghost";
        ConstructionGhost ghostScript = ghostObj.AddComponent<ConstructionGhost>();
        ghostScript.greenMat = green;
        ghostScript.redMat = red;
        // Ensure ghost doesn't have a collider that blocks raycasts or causes physics issues
        Collider col = ghostObj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        GameObject ghostPrefab = PrefabUtility.SaveAsPrefabAsset(ghostObj, "Assets/Prefabs/WoodWall_Ghost.prefab");

        GameObject.DestroyImmediate(wallObj);
        GameObject.DestroyImmediate(ghostObj);

        // 3. Scene Setup
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            BuildingManager manager = cam.GetComponent<BuildingManager>();
            if (manager == null) manager = cam.AddComponent<BuildingManager>();
            manager.buildPrefab = wallPrefab;
            manager.ghostPrefab = ghostPrefab;
            manager.cam = cam.GetComponent<Camera>();
            Debug.Log("BuildingManager attached to Main Camera.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Building System Setup Completed!");
    }
}
