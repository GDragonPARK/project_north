using UnityEngine;

public class Equipment_Manager : MonoBehaviour
{
    public static Equipment_Manager Instance { get; private set; }

    [Header("Sockets")]
    public Transform weaponSocketRight;
    public Transform weaponSocketLeft; // For shields or dual wield later

    private GameObject currentWeaponObject;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Try to find socket if not assigned (auto-setup)
        if (weaponSocketRight == null)
        {
            // Look for the socket created by Upgrader
            // We search in player children
            Transform foundSocket = FindDeepChild(transform, "Weapon_Socket");
            if (foundSocket) weaponSocketRight = foundSocket;
        }
    }

    public void EquipWeapon(ItemData weaponData)
    {
        if (weaponData == null || weaponData.weaponPrefab == null) return;

        // Unequip current
        UnequipWeapon();

        // Instantiate new
        if (weaponSocketRight != null)
        {
            currentWeaponObject = Instantiate(weaponData.weaponPrefab, weaponSocketRight);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.Euler(0, 90, 0); // Default grip rotation
            Debug.Log($"Equipped {weaponData.itemName}");
        }
        else
        {
            Debug.LogError("Weapon Socket not found! Cannot equip.");
        }
    }

    public void UnequipWeapon()
    {
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
            currentWeaponObject = null;
        }
        // Also clean up any other children of the socket in case
        if (weaponSocketRight != null && weaponSocketRight.childCount > 0)
        {
            foreach (Transform child in weaponSocketRight)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private Transform FindDeepChild(Transform aParent, string aName)
    {
        foreach(Transform child in aParent)
        {
            if(child.name == aName) return child;
            var result = FindDeepChild(child, aName);
            if (result != null) return result;
        }
        return null;
    }
}
