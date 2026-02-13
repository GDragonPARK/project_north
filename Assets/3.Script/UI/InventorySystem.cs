using UnityEngine;
using System.Collections.Generic;
using System;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Inventory Data")]
    public List<ItemData> inventoryList = new List<ItemData>();
    public int maxSlots = 20;

    public event Action OnItemChanged;
    
    public void ForceUIUpdate()
    {
        OnItemChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddItem(ItemData item)
    {
        if (inventoryList.Count < maxSlots)
        {
            inventoryList.Add(item);
            Debug.Log($"<color=green>[Inventory] Added {item.itemName}</color>");
            OnItemChanged?.Invoke();
            return true;
        }
        return false;
    }

    // Compatibility methods for existing systems
    public bool AddItem(ItemData item, int amount)
    {
        bool success = true;
        for (int i = 0; i < amount; i++)
        {
            if (!AddItem(item)) { success = false; break; }
        }
        return success;
    }

    public void RemoveItem(ItemData item)
    {
        if (inventoryList.Contains(item))
        {
            inventoryList.Remove(item);
            OnItemChanged?.Invoke();
        }
    }

    public void RemoveItem(ItemData item, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            RemoveItem(item);
        }
    }

    public bool HasItem(ItemData item, int amount)
    {
        int count = 0;
        foreach (var i in inventoryList)
        {
            if (i == item) count++;
        }
        return count >= amount;
    }

    // Resources Compatibility (Wood)
    public bool HasResources(int woodAmount)
    {
        return GetWoodCount() >= woodAmount;
    }

    public void ConsumeResources(int woodAmount)
    {
        int removed = 0;
        for (int i = inventoryList.Count - 1; i >= 0; i--)
        {
            if (inventoryList[i] != null && inventoryList[i].itemName == "Wood")
            {
                inventoryList.RemoveAt(i);
                removed++;
                if (removed >= woodAmount) break;
            }
        }
        OnItemChanged?.Invoke();
    }

    public void AddResources(int woodAmount)
    {
        // For compatibility, if we don't have a direct reference to Wood ItemData, 
        // this might fail. But we can try to find it.
        ItemData wood = Resources.Load<ItemData>("Items/Wood");
        if (wood != null) AddItem(wood, woodAmount);
    }

    public int GetWoodCount()
    {
        int count = 0;
        foreach (var item in inventoryList)
        {
            if (item != null && item.itemName == "Wood") count++;
        }
        return count;
    }

    // Legacy 'items' Dictionary access support - this is tricky with a List.
    // We can provide a property that generates a dictionary on the fly if needed for SaveManager.
    public Dictionary<ItemData, int> items
    {
        get
        {
            Dictionary<ItemData, int> dict = new Dictionary<ItemData, int>();
            foreach (var item in inventoryList)
            {
                if (item == null) continue;
                if (dict.ContainsKey(item)) dict[item]++;
                else dict[item] = 1;
            }
            return dict;
        }
    }
}
