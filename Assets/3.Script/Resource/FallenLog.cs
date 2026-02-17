using UnityEngine;
using System.Collections;

public class FallenLog : MonoBehaviour
{
    public GameObject lootPrefab;
    public int lootAmount = 3;
    public float delayBeforeLoot = 3.0f; // Delay before log disappears and spawns loot
    
    private void Start()
    {
        StartCoroutine(SpawnLootRoutine());
    }

    private IEnumerator SpawnLootRoutine()
    {
        yield return new WaitForSeconds(delayBeforeLoot);
        
        if (lootPrefab)
        {
            for (int i = 0; i < lootAmount; i++)
            {
                // Spawn with slight offset
                Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
                GameObject loot = Instantiate(lootPrefab, transform.position + offset, Quaternion.identity);
                
                // Inject Force Interaction Logic
                loot.AddComponent<ForceInteractionSetup>();
                
                // FORCE LAYER to "Item" (Layer 10)
                loot.layer = 10;

                // INJECT AUTO-PICKUP (The Missing Link)
                PickupItem pickup = loot.GetComponent<PickupItem>();
                if (pickup == null) pickup = loot.AddComponent<PickupItem>();
                
                // INJECT ITEM DATA (DNA)
                if (pickup.itemData == null)
                {
                    pickup.itemData = Resources.Load<ItemData>("Items/Wood");
                }

                // Debug.Log($"[FallenLog] Spawned {loot.name} with PickupItem & Data");

                // PRE-ADD GlowFilter (Disabled)
                var glow = loot.GetComponent<ChocDino.UIFX.GlowFilter>();
                if (glow == null) glow = loot.AddComponent<ChocDino.UIFX.GlowFilter>();
                
                if (glow != null)
                {
                    glow.Color = Color.yellow;
                    glow.Strength = 2.0f;
                    glow.enabled = false; // PlayerInteraction will enable it
                }
            }
        }
        
        Destroy(gameObject);
    }
}
