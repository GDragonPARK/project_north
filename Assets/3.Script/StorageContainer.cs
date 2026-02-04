using UnityEngine;
using System.Collections.Generic;

public class StorageContainer : MonoBehaviour
{
    [Header("Settings")]
    public string containerName = "Chest";
    public int slotCount = 10;
    
    // Using a list of stacks to represent slots (Valheim style)
    [SerializeField] private List<ResourceStack> m_storedItems = new List<ResourceStack>();

    public List<ResourceStack> GetItems() => m_storedItems;

    public void Open(Transform player)
    {
        Debug.Log($"<color=yellow>Opening {containerName}</color>");
        if (StorageUI.Instance != null)
        {
            StorageUI.Instance.OpenContainer(this);
        }
    }

    public void AddItem(ItemData item, int amount)
    {
        // Try to stack first
        for (int i = 0; i < m_storedItems.Count; i++)
        {
            if (m_storedItems[i].item == item)
            {
                ResourceStack stack = m_storedItems[i];
                stack.amount += amount;
                m_storedItems[i] = stack;
                return;
            }
        }

        // If not stacked and space available, add new stack
        if (m_storedItems.Count < slotCount)
        {
            m_storedItems.Add(new ResourceStack { item = item, amount = amount });
        }
    }

    public void RemoveItem(ItemData item, int amount)
    {
        for (int i = 0; i < m_storedItems.Count; i++)
        {
            if (m_storedItems[i].item == item)
            {
                ResourceStack stack = m_storedItems[i];
                stack.amount -= amount;
                if (stack.amount <= 0) m_storedItems.RemoveAt(i);
                else m_storedItems[i] = stack;
                return;
            }
        }
    }
}
