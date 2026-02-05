using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Settings")]
    public string hammerItemId = "Hammer";
    public GameObject buildMenuUI;
    public PlacementController placementController;
    public KeyCode toggleBuildKey = KeyCode.B;

    private bool isBuildMode = false;

    private void Update()
    {
        // 1. Check if holding Hammer
        // For now, check InventoryManager's current item or a placeholder
        // TODO: Hook into actual Equipment System
        // bool hasHammer = InventoryManager.Instance.IsEquipped(hammerItemId); 
        bool hasHammer = true; // Debug placeholder

        if (hasHammer && Input.GetKeyDown(toggleBuildKey))
        {
            ToggleBuildMode();
        }

        if (isBuildMode)
        {
            // Lock/Unlock cursor?
            if (buildMenuUI.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                 // Placement mode
                 Cursor.lockState = CursorLockMode.Locked;
                 Cursor.visible = false;
            }
        }
    }

    public void ToggleBuildMode()
    {
        isBuildMode = !isBuildMode;
        
        if (buildMenuUI) buildMenuUI.SetActive(isBuildMode);
        
        if (isBuildMode)
        {
            placementController.EnterBuildMode();
        }
        else
        {
            placementController.ExitBuildMode();
        }
    }
}
