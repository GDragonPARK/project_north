using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("Models")]
    public GameObject axeModel;
    public GameObject pickaxeModel;

    [Header("Components")]
    public WeaponDamageController weaponController;
    public Animator animator;

    private void Start()
    {
        // Init State
        if (axeModel) axeModel.SetActive(false);
        if (pickaxeModel) pickaxeModel.SetActive(false);

        // Default to Axe
        EquipAxe();
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        
        if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame) EquipAxe();
        if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame) EquipPickaxe();
    }

    public void EquipAxe()
    {
        if (axeModel) axeModel.SetActive(true);
        if (pickaxeModel) pickaxeModel.SetActive(false);

        if (weaponController)
        {
            weaponController.toolType = ToolType.Axe;
            weaponController.damage = 20f; // Axe Damage
        }
        
        // Optional: Set Animator Layer or Bool
        // animator.SetInteger("WeaponType", 1);
        Debug.Log("Equipped Axe");
    }

    public void EquipPickaxe()
    {
        if (axeModel) axeModel.SetActive(false);
        if (pickaxeModel) pickaxeModel.SetActive(true);

        if (weaponController)
        {
            weaponController.toolType = ToolType.Pickaxe;
            weaponController.damage = 15f; // Pickaxe Damage
        }

        // Optional: Set Animator Layer or Bool
        // animator.SetInteger("WeaponType", 2);
        Debug.Log("Equipped Pickaxe");
    }
}
