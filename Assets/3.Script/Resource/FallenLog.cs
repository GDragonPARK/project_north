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
                
                // FORCE LAYER to "Item"
                loot.layer = LayerMask.NameToLayer("Item");

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
