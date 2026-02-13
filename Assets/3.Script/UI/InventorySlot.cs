using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

public class InventorySlot : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    private ItemData item;

    public void SetItem(ItemData newItem)
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
            if (itemNameText != null) itemNameText.text = item.itemName;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(0, 0, 0, 0);
            if (itemNameText != null) itemNameText.text = "";
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
