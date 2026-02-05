using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Quick Slots (1-4)")]
    public List<SlotUI> quickSlots; // Assign 4 slots in Inspector

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshQuickSlots;
            RefreshQuickSlots();
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshQuickSlots;
        }
    }

    public void RefreshQuickSlots()
    {
        for (int i = 0; i < quickSlots.Count; i++)
        {
            if (i < InventoryManager.Instance.totalSlots)
            {
                // Assuming slots 0-3 are the quick slots
                var slotData = InventoryManager.Instance.GetQuickSlot(i);
                quickSlots[i].SetSlot(slotData);
            }
        }
    }
}
