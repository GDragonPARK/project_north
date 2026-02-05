using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public GameObject selectionOutline;

    private InventoryManager.SlotData currentSlot;

    public void SetSlot(InventoryManager.SlotData slot)
    {
        currentSlot = slot;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (currentSlot == null || currentSlot.IsEmpty)
        {
            iconImage.enabled = false;
            amountText.enabled = false;
        }
        else
        {
            iconImage.enabled = true;
            iconImage.sprite = currentSlot.item.icon;
            
            amountText.enabled = currentSlot.amount > 1;
            amountText.text = currentSlot.amount.ToString();
        }
    }
}
