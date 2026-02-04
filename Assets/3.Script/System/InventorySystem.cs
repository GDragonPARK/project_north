using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Inventory Data")]
    public Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
    
    // For Inspector visibility/initialization (optional)
    public List<ResourceStack> startingItems;

    public System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var stack in startingItems)
        {
            AddItem(stack.item, stack.amount);
        }
    }

    // Helper for legacy wood support if needed
    public int woodCount
    {
        get {
            foreach(var pair in items) {
                if (pair.Key.itemName == "Wood") return pair.Value;
            }
            return 0;
        }
    }

    public void AddItem(ItemData item, int amount)
    {
        if (item == null) return;
        
        if (items.ContainsKey(item)) items[item] += amount;
        else items[item] = amount;

        Debug.Log($"<color=green>Added {amount}x {item.itemName}</color>");
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(ItemData item, int amount)
    {
        if (item == null) return false;
        return items.ContainsKey(item) && items[item] >= amount;
    }

    public bool HasResources(int woodAmount) // Keep for BuildManager compatibility
    {
        foreach(var pair in items) {
            if (pair.Key.itemName == "Wood") return pair.Value >= woodAmount;
        }
        return false;
    }

    public void ConsumeResources(int woodAmount) // Keep for BuildManager compatibility
    {
        ItemData woodItem = null;
        foreach(var pair in items) {
            if (pair.Key.itemName == "Wood") { woodItem = pair.Key; break; }
        }
        if (woodItem != null) RemoveItem(woodItem, woodAmount);
    }

    public void AddResources(int woodAmount) // Keep for BuildManager compatibility
    {
         // Find wood item in buildable pieces or load from resources
         // For now, it's safer to just handle it via AddItem if the caller provides the data
    }

    public void RemoveItem(ItemData item, int amount)
    {
        if (item == null) return;
        if (items.ContainsKey(item))
        {
            items[item] -= amount;
            if (items[item] <= 0) items.Remove(item);
            OnInventoryChanged?.Invoke();
        }
    }
}
