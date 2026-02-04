using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public delegate void OnInventoryChanged();
    public OnInventoryChanged onInventoryChanged;

    private Dictionary<string, int> items = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName))
            items[itemName] += amount;
        else
            items.Add(itemName, amount);

        Debug.Log($"Added {amount} {itemName}. Total: {items[itemName]}");
        onInventoryChanged?.Invoke();
        
        // Notification logic would go here
    }

    public bool HasItem(string itemName, int amount)
    {
        return items.ContainsKey(itemName) && items[itemName] >= amount;
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (HasItem(itemName, amount))
        {
            items[itemName] -= amount;
            onInventoryChanged?.Invoke();
        }
    }

    public int GetItemCount(string itemName)
    {
        return items.ContainsKey(itemName) ? items[itemName] : 0;
    }

    public void UseItem(ItemData item)
    {
        if (item == null || !HasItem(item.itemName, 1)) return;

        if (item.type == ItemData.ItemType.Food)
        {
            if (CharacterStats.Instance != null)
            {
                CharacterStats.Instance.Heal(item.healthBonus);
                CharacterStats.Instance.BoostMaxStamina(item.staminaBonus, item.duration);
                RemoveItem(item.itemName, 1);
                Debug.Log($"Consumed {item.itemName}. HP recovered, Stamina boosted!");
            }
        }
        else if (item.type == ItemData.ItemType.Weapon)
        {
            if (Equipment_Manager.Instance != null)
            {
                Equipment_Manager.Instance.EquipWeapon(item);
                // Do not remove item? Or move to "Equipped" slot? 
                // For now, keep in inventory but equip visual.
                Debug.Log($"Equipped weapon: {item.itemName}");
            }
        }
    }
}
