using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingManager : MonoBehaviour
{
    public GameObject buildPrefab;
    public GameObject ghostPrefab;
    public Camera cam;
    public float buildDistance = 10f;

    private GameObject currentGhost;
    private bool isBuildingMode = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ToggleBuildingMode();
        }

        if (isBuildingMode)
        {
            UpdateGhostPosition();
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && currentGhost.GetComponent<ConstructionGhost>().canBuild)
            {
                Build();
            }
        }
    }

    void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode)
        {
            currentGhost = Instantiate(ghostPrefab);
        }
        else
        {
            if (currentGhost != null) Destroy(currentGhost);
        }
    }

    void UpdateGhostPosition()
    {
        if (cam == null) cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, buildDistance))
        {
            currentGhost.transform.position = hit.point;
            currentGhost.transform.up = hit.normal;
            currentGhost.SetActive(true);
        }
        else
        {
            currentGhost.SetActive(false);
        }
    }

    void Build()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("Wood", 2))
        {
            if (CharacterStats.Instance != null)
            {
                if (!CharacterStats.Instance.HasEnoughStamina(20f))
                {
                    Debug.Log("Not enough stamina to build!");
                    return;
                }
                CharacterStats.Instance.UseStamina(20f);
            }

            InventoryManager.Instance.RemoveItem("Wood", 2);
            Instantiate(buildPrefab, currentGhost.transform.position, currentGhost.transform.rotation);
            ToggleBuildingMode();
            Debug.Log("Build successful! 2 Wood consumed.");
        }
        else
        {
            Debug.Log("Not enough Wood!");
        }
    }
}
