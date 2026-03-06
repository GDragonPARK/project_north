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
        return GetCount(item) >= amount;
    }

    public int GetCount(ItemData item)
    {
        if (item == null) return 0;
        int count = 0;
        foreach (var i in inventoryList)
        {
            if (i == item) count++;
        }
        return count;
    }

    public bool TryConsume(ItemData item, int amount)
    {
        if (GetCount(item) < amount) return false;

        int removed = 0;
        // Iterate backwards to safely remove
        for (int i = inventoryList.Count - 1; i >= 0; i--)
        {
            if (inventoryList[i] == item)
            {
                inventoryList.RemoveAt(i);
                removed++;
                if (removed >= amount) break;
            }
        }
        OnItemChanged?.Invoke();
        return true;
    }

/// <summary>[Phase 9.1] itemName 문자열로 보유 수량 확인</summary>
    public bool HasItem(string itemName, int amount)
    {
        int count = 0;
        foreach (var item in inventoryList)
        {
            if (item != null && item.itemName == itemName) count++;
            if (count >= amount) return true;
        }
        return false;
    }

    /// <summary>[Phase 9.1] itemName 문자열로 아이템 amount개 차감 후 UI 이벤트 발생</summary>
/// <summary>[Phase 9.1 Fixed] itemName으로 amount만큼 정확히 차감. 잔량이 0 이하일 때만 슬롯 제거.</summary>
    public bool ConsumeItem(string itemName, int amount)
    {
        if (!HasItem(itemName, amount)) return false;

        int remaining = amount;
        for (int i = inventoryList.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = inventoryList[i];
            if (item == null || item.itemName != itemName) continue;

            if (item.amount > remaining)
            {
                // 수량만 차감, 슬롯 유지
                item.amount -= remaining;
                remaining = 0;
            }
            else
            {
                // 수량이 부족하거나 딱 떨어짐 때만 슬롯 삭제
                remaining -= item.amount;
                inventoryList.RemoveAt(i);
            }
        }

        OnItemChanged?.Invoke();
        return true;
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
        // Improved to use GetCount if possible, but keep string check for safety
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
