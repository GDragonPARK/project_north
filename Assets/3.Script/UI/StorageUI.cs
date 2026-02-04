using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class StorageUI : MonoBehaviour
{
    public static StorageUI Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject storagePanel;
    public Text containerNameText;
    
    [Header("Grids")]
    public Transform containerContent;
    public Transform playerContent;
    public GameObject slotPrefab;

    private StorageContainer m_currentContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (storagePanel != null) storagePanel.SetActive(false);
    }

    public void OpenContainer(StorageContainer container)
    {
        m_currentContainer = container;
        containerNameText.text = container.containerName;
        storagePanel.SetActive(true);
        
        RefreshUI();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        storagePanel.SetActive(false);
        m_currentContainer = null;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (storagePanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void RefreshUI()
    {
        if (m_currentContainer == null) return;

        // Clear grids
        foreach (Transform child in containerContent) Destroy(child.gameObject);
        foreach (Transform child in playerContent) Destroy(child.gameObject);

        // Populate Container Items
        foreach (var stack in m_currentContainer.GetItems())
        {
            CreateSlot(stack, containerContent, true);
        }

        // Populate Player Items
        if (InventorySystem.Instance != null)
        {
            foreach (var pair in InventorySystem.Instance.items)
            {
                ResourceStack stack = new ResourceStack { item = pair.Key, amount = pair.Value };
                CreateSlot(stack, playerContent, false);
            }
        }
    }

    private void CreateSlot(ResourceStack stack, Transform parent, bool isFromContainer)
    {
        GameObject slotObj = Instantiate(slotPrefab, parent);
        Text txt = slotObj.GetComponentInChildren<Text>();
        if (txt != null) txt.text = $"{stack.item.itemName} ({stack.amount})";

        // Add Tooltip Support
        TooltipTrigger trigger = slotObj.AddComponent<TooltipTrigger>();
        trigger.item = stack.item;

        Button btn = slotObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => TransferItem(stack, isFromContainer));
        }
    }

    private void TransferItem(ResourceStack stack, bool isFromContainer)
    {
        if (isFromContainer)
        {
            // From Container to Player
            m_currentContainer.RemoveItem(stack.item, stack.amount);
            InventorySystem.Instance.AddItem(stack.item, stack.amount);
        }
        else
        {
            // From Player to Container
            InventorySystem.Instance.RemoveItem(stack.item, stack.amount);
            m_currentContainer.AddItem(stack.item, stack.amount);
        }

        RefreshUI();
    }
}
