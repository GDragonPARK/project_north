using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

public class InventorySlot : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemAmountText; // NEW
    private ItemData item;

    public void SetItem(ItemData newItem, int amount)
    {
        item = newItem;
        if (item != null)
        {
            if (item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.color = Color.white; 
            }
            else
            {
                itemIcon.color = new Color(0, 0, 0, 0); 
            }
            
            // Name text removed for icon-only grid style
            if (itemNameText != null) itemNameText.text = ""; 
            
            if (itemAmountText != null)
            {
                itemAmountText.text = amount > 1 ? $"x{amount}" : "";
            }
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(0, 0, 0, 0);
            if (itemNameText != null) itemNameText.text = "";
            if (itemAmountText != null) itemAmountText.text = "";
        }
    }
    
    public void OnClickSlot()
    {
        if (item == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ThirdPersonController pc = player.GetComponent<ThirdPersonController>();
            if (pc != null)
            {
                pc.AddQuickSlot(item);
                pc.EquipItem(item);
            }
        }
    }
}
