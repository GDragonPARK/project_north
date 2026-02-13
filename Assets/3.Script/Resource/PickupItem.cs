using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SphereCollider))]
public class PickupItem : MonoBehaviour
{
    public string itemName = "Wood";
    public int amount = 1;

    [Tooltip("Add this tag to allow PlayerInteraction to find it via raycast? Or use Trigger.")]
    // User requested E key interaction. 
    // Usually E key uses Raycast or Trigger + "Press E".
    // PlayerInteraction.cs uses Raycast -> ResourceObject.
    // We should make PickupItem work with PlayerInteraction or trigger.
    
    // Let's implement ResourceObject interface-like behavior or just a method to be called.
    
    public InventoryItem itemData; // Assign in Editor
    
    // Start removed to avoid tag errors. Rely on component detection.

    public void Interact()
    {
        if (InventoryManager.Instance)
        {
            if (itemData != null)
            {
                if (InventoryManager.Instance.AddItem(itemData, amount))
                {
                    Debug.Log($"Picked up {amount} {itemData.name}");
                    // Show UI Notification (User Request)
                    // NotificationManager.Show($"+{amount} {itemData.name}"); 
                    Debug.Log($"<color=green>+ {amount} {itemData.name}</color>");
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    Debug.Log("Inventory Full");
                    return;
                }
            }
            
            // Fallback to Resources if itemData missing
            InventoryItem item = Resources.Load<InventoryItem>("Items/" + itemName);
            if (item)
            {
                if (InventoryManager.Instance.AddItem(item, amount))
                {
                    Debug.Log($"Picked up {amount} {item.name}");
                    Debug.Log($"<color=green>+ {amount} {item.name}</color>");
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Inventory Full");
                }
            }
            else
            {
                Debug.LogWarning($"Item Data missing on {gameObject.name}");
                Destroy(gameObject); // Destroy to prevent stuck item
            }
        }
    }

}
