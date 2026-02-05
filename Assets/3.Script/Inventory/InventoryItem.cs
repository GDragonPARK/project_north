using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class InventoryItem : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    public int maxStackSize = 10;
    public bool isEquippable;
    public GameObject prefab; // For dropping or equipping
}
