using UnityEngine;
using UnityEditor;
using StarterAssets;

public class PhysicsLayerFixer : EditorWindow
{
    [MenuItem("Tools/Fix Layers and Physics")]
    public static void ApplyFixes()
    {
        // 1. Create Ground Layer if missing
        string groundLayerName = "Ground";
        int groundLayerIndex = LayerMask.NameToLayer(groundLayerName);
        
        if (groundLayerIndex == -1)
        {
            // Try to find an empty layer
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = groundLayerName;
                    tagManager.ApplyModifiedProperties();
                    groundLayerIndex = i;
                    Debug.Log($"Created Layer '{groundLayerName}' at index {i}");
                    break;
                }
            }
        }
        
        if (groundLayerIndex == -1)
        {
            Debug.LogError("Could not create Ground Layer! All layers full?");
            return;
        }

        // 2. Assign Terrain to Ground Layer
        Terrain terrain = FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            SetLayerRecursively(terrain.gameObject, groundLayerIndex);
            
            // Ensure Collider
            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc == null) terrain.gameObject.AddComponent<TerrainCollider>().terrainData = terrain.terrainData;
            else tc.terrainData = terrain.terrainData; // Refresh

            Debug.Log($"Assigned Terrain to Layer '{groundLayerName}' ({groundLayerIndex}) and refreshed Collider.");
        }
        else
        {
            Debug.LogError("No Active Terrain found!");
        }

        // 3. Isolate Black Platform (StartupFloor / Plane)
        // Adjust this if the name is different
        string[] floorNames = { "StartupFloor", "Floor", "Plane", "Environment_Manager" }; 
        foreach (string name in floorNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                // Set to Default (0) or Environment (if you have it)
                // Let's us Default to match instructions "Not Ground"
                SetLayerRecursively(obj, 0); 
                Debug.Log($"Set {name} to Default Layer (Ignored by TPC).");
            }
        }

        // 4. Update Player Physics
        GameObject player = GameObject.Find("Player_New");
        if (player)
        {
            // TPC Ground Mask
            ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
            if (tpc)
            {
                tpc.GroundLayers = 1 << groundLayerIndex; // Only Ground
                Debug.Log($"Updated ThirdPersonController GroundLayers to '{groundLayerName}' only.");
            }

            // CharacterController Tuning
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc)
            {
                cc.stepOffset = 0.5f;
                cc.skinWidth = 0.08f;
                // cc.minMoveDistance = 0; // Optional for jitter
                Debug.Log("Tuned CharacterController: StepOffset=0.5, SkinWidth=0.08");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (null == obj) return;
        
        obj.layer = newLayer;
        
        foreach (Transform child in obj.transform)
        {
            if (null == child) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}