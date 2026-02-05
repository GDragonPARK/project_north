using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Settings")]
    public int totalSlots = 20;

    // Inventory Data (Model)
    // Slot Index -> Item + Amount
    public class SlotData
    {
        public InventoryItem item;
        public int amount;
        public float durability;

        public bool IsEmpty => item == null;
    }

    public List<SlotData> inventorySlots = new List<SlotData>();

    // Events (Observer Pattern)
    public event Action OnInventoryChanged;
    public event Action<int> OnSlotChanged; // Index specific

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Initialize Empty Slots
        for (int i = 0; i < totalSlots; i++)
        {
            inventorySlots.Add(new SlotData());
        }
    }

    // Auto-Loot Logic
    public bool AddItem(InventoryItem item, int amount = 1)
    {
        if (item == null)
        {
            Debug.LogError("[InventoryManager] Attempted to add NULL item.");
            return false;
        }

        // 1. Stackable check
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.item == item && slot.amount < item.maxStackSize)
            {
                int space = item.maxStackSize - slot.amount;
                int toAdd = Mathf.Min(space, amount);
                slot.amount += toAdd;
                amount -= toAdd;
                OnInventoryChanged?.Invoke();
                if (amount <= 0) return true;
            }
        }

        // 2. Empty Slot check
        if (amount > 0)
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (inventorySlots[i].IsEmpty)
                {
                    inventorySlots[i].item = item;
                    inventorySlots[i].amount = amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        return false; // Inventory Full
    }
    
    // Quick Slot Accessors for HUD
    public SlotData GetQuickSlot(int index) // 0-3 for slots 1-4
    {
        if (index >= 0 && index < inventorySlots.Count)
            return inventorySlots[index];
        return null;
    }

    // Helper: Check for item by ID/Name
    public bool HasItem(string itemId, int amount)
    {
        int count = 0;
        foreach (var slot in inventorySlots)
        {
            if (!slot.IsEmpty && (slot.item.id == itemId || slot.item.itemName == itemId))
            {
                count += slot.amount;
            }
        }
        return count >= amount;
    }

    // Helper: Remove item by ID/Name
    public void RemoveItem(string itemId, int amount)
    {
        // First verify we have enough (optional, but consistent)
        if (!HasItem(itemId, amount)) return;

        int remaining = amount;
        foreach (var slot in inventorySlots)
        {
            if (remaining <= 0) break;

            if (!slot.IsEmpty && (slot.item.id == itemId || slot.item.itemName == itemId))
            {
                int take = Mathf.Min(remaining, slot.amount);
                slot.amount -= take;
                remaining -= take;

                if (slot.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }
            }
        }
        OnInventoryChanged?.Invoke();
    }
}
