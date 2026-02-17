using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class BuildingPhysicsSetup
{
    static BuildingPhysicsSetup()
    {
        EditorApplication.delayCall += SetupCollisionMatrix;
    }

    [MenuItem("Tools/Building System/Setup Collision Matrix")]
    public static void SetupCollisionMatrix()
    {
        int playerLayer = LayerMask.NameToLayer("Default"); // Player_New is on Default
        int buildingLayer = LayerMask.NameToLayer("Building");
        int previewLayer = LayerMask.NameToLayer("BuildingPreview");

        if (buildingLayer == -1 || previewLayer == -1)
        {
            Debug.LogError("Building or BuildingPreview layer not found. Please add them first.");
            return;
        }

        // BuildingPreview should NOT collide with Player or Building
        Physics.IgnoreLayerCollision(previewLayer, playerLayer, true);
        Physics.IgnoreLayerCollision(previewLayer, buildingLayer, true);
        
        // Optionally ignore itself
        Physics.IgnoreLayerCollision(previewLayer, previewLayer, true);

        Debug.Log("Building System: Collision Matrix configured. BuildingPreview will ignore Player and Building layers.");
    }
}
