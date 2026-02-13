using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public string itemName = "Wood";
    public int amount = 1;
    public ItemData itemData;

    public void PickUp()
    {
        if (InventorySystem.Instance != null)
        {
            // If itemData is not set, try to find it in Resources
            if (itemData == null)
            {
                itemData = Resources.Load<ItemData>("Items/" + itemName);
            }

            if (itemData != null)
            {
                if (InventorySystem.Instance.AddItem(itemData))
                {
                    Debug.Log($"Picked up {amount} {itemData.itemName}");
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"ItemData for {itemName} not found!");
                Destroy(gameObject); 
            }
        }
    }

    public string GetInteractionMessage()
    {
        return $"[E] {itemName} 줍기";
    }
}
