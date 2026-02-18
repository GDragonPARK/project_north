#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectSetupTool : EditorWindow
{
    [MenuItem("Tools/Project North/Fix Physics Setup")]
    public static void FixPhysicsSetup()
    {
        // 1. Add "Item" Layer
        int itemLayer = CreateLayer("Item");
        if (itemLayer == -1)
        {
            Debug.LogError("[ProjectSetup] Failed to create Item layer!");
            return;
        }

        // 2. Set Physics Matrix (Ignore collision between Player and Item)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1)
        {
            Debug.LogWarning("[ProjectSetup] Player layer not found! Creating it...");
            playerLayer = CreateLayer("Player");
        }

        Physics.IgnoreLayerCollision(playerLayer, itemLayer, true);
        Debug.Log($"[ProjectSetup] Physics Matrix: Player({playerLayer}) <-> Item({itemLayer}) collision IGNORED");

        // 3. Update Wood Prefab Layer
        UpdatePrefabLayer("Assets/Resources/Items/Wood.prefab", itemLayer);
        
        // Save project settings
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ProjectSetup] ✅ Physics Setup Complete: Layer & Matrix Fixed");
    }

    [MenuItem("Tools/Project North/Auto Link Building Data")]
    public static void AutoLinkBuildingData()
    {
        // 1. Find all BuildingCategorySO assets
        string[] guids = AssetDatabase.FindAssets("t:BuildingCategorySO");
        System.Collections.Generic.List<BuildingCategorySO> foundCategories = new System.Collections.Generic.List<BuildingCategorySO>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingCategorySO category = AssetDatabase.LoadAssetAtPath<BuildingCategorySO>(path);
            if (category != null)
            {
                foundCategories.Add(category);
            }
        }

        if (foundCategories.Count == 0)
        {
            Debug.LogWarning("[ProjectSetup] No BuildingCategorySO found in project.");
            return;
        }

        // 2. Find BuildingManager in scene
        BuildingManager manager = Object.FindAnyObjectByType<BuildingManager>();
        if (manager == null)
        {
            Debug.LogError("[ProjectSetup] BuildingManager not found in scene!");
            return;
        }

        // 3. Link categories
        Undo.RecordObject(manager, "Auto Link Building Data");
        manager.categories = foundCategories;
        
        // 4. Save
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ProjectSetup] ✅ Found {foundCategories.Count} categories and linked to BuildingManager.");
    }

    private static int CreateLayer(string layerName)
    {
        // Check if layer already exists
        int existingLayer = LayerMask.NameToLayer(layerName);
        if (existingLayer != -1)
        {
            Debug.Log($"[ProjectSetup] Layer '{layerName}' already exists at index {existingLayer}");
            return existingLayer;
        }

        // Load TagManager
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Find empty slot (User layers are 8-31, but 8-10 are often reserved)
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[ProjectSetup] Created layer '{layerName}' at index {i}");
                return i;
            }
        }

        Debug.LogError($"[ProjectSetup] No empty layer slots available for '{layerName}'!");
        return -1;
    }

    private static void UpdatePrefabLayer(string prefabPath, int layer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[ProjectSetup] Prefab not found at: {prefabPath}");
            return;
        }

        // Load as editable instance
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(assetPath);

        // Set layer recursively
        SetLayerRecursively(prefabInstance, layer);

        // Save changes
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log($"[ProjectSetup] Updated prefab layer: {prefabPath} -> Layer {layer}");
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
