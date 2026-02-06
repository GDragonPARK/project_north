using UnityEngine;
using System.Collections;

public class FallenTreeLoot : MonoBehaviour
{
    [Header("Loot Settings")]
    public string resourceName = "Wood"; // Item name to spawn (assuming ItemPickup or similar system)
    public GameObject itemPrefab; // Prefab to spawn if not using a manager
    public int dropCount = 3;
    public float lifeTime = 3.0f;

    private void Start()
    {
        StartCoroutine(LootRoutine());
    }

    private IEnumerator LootRoutine()
    {
        // Wait for physics to settle or just time
        yield return new WaitForSeconds(lifeTime);

        // Spawn Loot
        SpawnLoot();

        // Disappear
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        // Check if we have a prefab directly assigned
        if (itemPrefab != null)
        {
            for (int i = 0; i < dropCount; i++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
                Instantiate(itemPrefab, transform.position + randomOffset, Quaternion.identity);
            }
        }
        else
        {
            // Fallback: Check if there's a global ItemManager or similar
            // For now, just log if no prefab. 
            // In a real scenario, we might call InventorySystem.SpawnItem("Wood")
            Debug.Log($"[FallenTreeLoot] Spawning {dropCount} x {resourceName} directly (No prefab assigned, logic placeholder).");
            
            // Try to find a resource prefab in Resources if needed, or just warn.
            // Assuming the user will assign the prefab in the Inspector or we rely on the implementation plan's "ObjectPool" or similar.
        }
    }
}
