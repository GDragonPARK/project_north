using UnityEngine;

public class ResourceObject : MonoBehaviour
{
    public ItemData dropItem;
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
            if (dropItem != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(dropItem.itemName, 1);
            }
            Destroy(gameObject);
        }
    }
}
