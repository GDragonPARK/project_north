using UnityEngine;
using System.Collections;

public class FallenTreeLoot : MonoBehaviour
{
    [Header("Loot Settings")]
    public string resourceName = "Wood"; 
    public GameObject itemPrefab; 
    public int dropCount = 3;
    public float lifeTime = 3.0f;

    private void Start()
    {
        StartCoroutine(LootRoutine());
    }

    private IEnumerator LootRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        SpawnLoot();
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        for (int i = 0; i < dropCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            Vector3 spawnPos = transform.position + randomOffset;
            
            GameObject loot = null;

            if (itemPrefab != null)
            {
                loot = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: Create generic 'Wood' drop
                loot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                loot.transform.position = spawnPos;
                loot.transform.localScale = Vector3.one * 0.3f;
                loot.name = resourceName + "_Chunk";
                
                var renderer = loot.GetComponent<Renderer>();
                if (renderer) renderer.material.color = new Color(0.6f, 0.4f, 0.2f); // Brown
            }

            // Ensure Physics
            if (loot.GetComponent<Rigidbody>() == null)
            {
                var rb = loot.AddComponent<Rigidbody>();
                rb.mass = 1f;
            }
            if (loot.GetComponent<Collider>() == null)
            {
                loot.AddComponent<SphereCollider>();
            }

            // Ensure Interaction Component
            ItemObject itemObj = loot.GetComponent<ItemObject>();
            if (itemObj == null) itemObj = loot.AddComponent<ItemObject>();
            
            itemObj.itemName = resourceName;
            itemObj.amount = 1;
            
            // Try load data
            if (itemObj.itemData == null)
                itemObj.itemData = Resources.Load<ItemData>($"Items/{resourceName}");

            // Set Layer to Item if exists
            int itemLayer = LayerMask.NameToLayer("Item");
            if (itemLayer != -1) loot.layer = itemLayer;
        }
    }
}
