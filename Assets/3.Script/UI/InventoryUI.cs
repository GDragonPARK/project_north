using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public TextMeshProUGUI inventoryText;
    public GameObject m_slotPrefab;
    public Transform m_gridParent;
    [Header("UI Components")]
    public GameObject inventoryPanel; 
    
    [Header("Inventory Settings")]
    [SerializeField] private int m_slotCount = 20;

    private List<GameObject> m_slots = new List<GameObject>();

    private void Awake()
    {
        // FORCE DISABLE ON AWAKE
        if (inventoryPanel == null)
        {
            Transform panelTrans = transform.Find("Inventory_Panel");
            if (panelTrans != null) inventoryPanel = panelTrans.gameObject;
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("InventoryUI: Forced Panel Disable on Awake");
        }
    }

    private void Start()
    {
        // Try to find the panel if not assigned
        if (inventoryPanel == null)
        {
            Transform panelTrans = transform.Find("Inventory_Panel");
            if (panelTrans != null) inventoryPanel = panelTrans.gameObject;
        }

        InitializeInventory();
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnItemChanged += RefreshUI;
        }
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("InventoryUI: Forced Panel Disable on Start");
        }
        else
        {
            Debug.LogError("InventoryUI: Panel is NULL on Start!");
        }

        // Start locked
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RefreshUI();
    }

    private void InitializeInventory()
    {
        if (m_slotPrefab == null || m_gridParent == null) return;

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

    private void RefreshUI()
    {
        if (InventorySystem.Instance == null) return;

        List<ItemData> items = InventorySystem.Instance.inventoryList;

        for (int i = 0; i < m_slots.Count; i++)
        {
            InventorySlot slot = m_slots[i].GetComponent<InventorySlot>();
            if (slot == null) slot = m_slots[i].GetComponentInChildren<InventorySlot>();

            if (slot != null)
            {
                if (i < items.Count)
                {
                    slot.SetItem(items[i]);
                }
                else
                {
                    slot.SetItem(null);
                }
            }
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isActive);

        if (isActive)
        {
            RefreshUI();
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
