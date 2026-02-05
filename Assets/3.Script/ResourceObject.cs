using UnityEngine;

public class ResourceObject : MonoBehaviour
{
    public InventoryItem dropItem;
    public int health = 3;

    public void Gather()
    {
        if (CharacterStats.Instance != null)
        {
            if (!CharacterStats.Instance.HasEnoughStamina(10f))
            {
                Debug.Log("Not enough stamina to gather!");
                return;
            }
            CharacterStats.Instance.UseStamina(10f);
        }

        health--;
        Debug.Log($"Gathering {(dropItem != null ? dropItem.itemName : "Item")}. Remaining health: {health}");
        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} destroyed. Received {(dropItem != null ? dropItem.itemName : "Item")}!");
            
            if (dropItem != null)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(dropItem, 1);
                }
                else
                {
                    Debug.LogError("[ResourceObject] InventoryManager Instance is NULL! Cannot add item.");
                }
            }
            else
            {
                 Debug.LogWarning($"[ResourceObject] No 'Drop Item' assigned for {gameObject.name}.");
            }

            Destroy(gameObject);
        }
    }
}
