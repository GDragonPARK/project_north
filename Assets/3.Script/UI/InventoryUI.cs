using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public TextMeshProUGUI inventoryText;
    public GameObject m_slotPrefab;
    public Transform m_gridParent;
    [SerializeField] private int m_slotCount = 20;

    private List<GameObject> m_slots = new List<GameObject>();

    private void Start()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        if (m_slotPrefab == null)
        {
            Debug.LogError($"InventoryUI on [ {gameObject.name} ] : m_slotPrefab is NOT assigned! Please check the Inspector.", gameObject);
            return;
        }
        if (m_gridParent == null)
        {
            Debug.LogError($"InventoryUI on [ {gameObject.name} ] : m_gridParent is NOT assigned! Please check the Inspector.", gameObject);
            return;
        }

        Debug.Log($"InventoryUI on [ {gameObject.name} ] : Initializing with prefab {m_slotPrefab.name}");

        // Clear existing
        foreach (var slot in m_slots) if(slot != null) Destroy(slot);
        m_slots.Clear();

        // Create empty slots
        for (int i = 0; i < m_slotCount; i++)
        {
            GameObject slot = Instantiate(m_slotPrefab, m_gridParent);
            slot.name = $"Slot_{i}";
            m_slots.Add(slot);
        }
    }

    public void ToggleInventory()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf)
        {
            // Lock cursor or other logic
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
