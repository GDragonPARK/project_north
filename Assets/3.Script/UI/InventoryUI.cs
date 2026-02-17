using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

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
        if (inventoryPanel == null)
        {
            Transform panelTrans = transform.Find("Inventory_Panel");
            if (panelTrans != null) inventoryPanel = panelTrans.gameObject;
        }

        // Initialize Grid Parent
        if (m_gridParent == null && inventoryPanel != null)
        {
            m_gridParent = inventoryPanel.transform.Find("Scroll View/Viewport/Content");
            if (m_gridParent == null) m_gridParent = inventoryPanel.transform.Find("Content"); 
            // Fallback for flat hierarchy (ValheimUI_Builder style)
            if (m_gridParent == null) m_gridParent = inventoryPanel.transform; 
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnItemChanged += RefreshUI;
        }
        
        if (inventoryPanel != null)
        {
            // Prepare Grid (Empty Slots)
            InitializeGrid();
            inventoryPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("InventoryUI: Panel is NULL on Start!");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeGrid()
    {
        if (m_gridParent == null || m_slotPrefab == null) return;

        // Clear any dev placeholder
        foreach (Transform child in m_gridParent) Destroy(child.gameObject);
        m_slots.Clear();

        // Instantiate Fixed Count
        for (int i = 0; i < m_slotCount; i++)
        {
            GameObject slotObj = Instantiate(m_slotPrefab, m_gridParent);
            InventorySlot slotScript = slotObj.GetComponent<InventorySlot>();
            if (slotScript == null) slotScript = slotObj.GetComponentInChildren<InventorySlot>();
            
            // Initialize Empty
            if (slotScript != null) slotScript.SetItem(null, 0);

            m_slots.Add(slotObj);
        }
    }

    private void RefreshUI()
    {
        if (InventorySystem.Instance == null) return;

        var groupedItems = new List<KeyValuePair<ItemData, int>>(InventorySystem.Instance.items);
        
        for (int i = 0; i < m_slots.Count; i++)
        {
            GameObject slotObj = m_slots[i];
            InventorySlot slotScript = slotObj.GetComponent<InventorySlot>();
            if (slotScript == null) slotScript = slotObj.GetComponentInChildren<InventorySlot>();

            if (slotScript != null)
            {
                if (i < groupedItems.Count)
                {
                    // Valid Item
                    slotScript.SetItem(groupedItems[i].Key, groupedItems[i].Value);
                }
                else
                {
                    // Empty Slot
                    slotScript.SetItem(null, 0);
                }
            }
        }
    }

    private float _debugTimer;
    private void Update()
    {
        // Toggle Inventory
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }

        // DEBUG: Force Refresh every 1s to check data
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer > 1.0f)
            {
                _debugTimer = 0;
                RefreshUI();
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
            Debug.Log($"Inventory Opened. Items: {InventorySystem.Instance.items.Count}");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
