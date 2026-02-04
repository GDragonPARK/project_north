using UnityEngine;

public class BossSummonAltar : MonoBehaviour
{
    [Header("Requirements")]
    public ItemData tributeItem;
    public int tributeAmount = 1;
    public GameObject bossPrefab;
    public Transform spawnPoint;

    public float interactRange = 5f;

    public void Interact()
    {
        if (InventorySystem.Instance.HasItem(tributeItem, tributeAmount))
        {
            InventorySystem.Instance.RemoveItem(tributeItem, tributeAmount);
            SummonBoss();
        }
        else
        {
            Debug.Log($"<color=orange>You need {tributeAmount}x {tributeItem.itemName} to summon the boss.</color>");
        }
    }

    private void SummonBoss()
    {
        if (bossPrefab != null)
        {
            Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("<color=red>THE BOSS HAS ARRIVED!</color>");
            // Trigger weather change or music here
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.currentWeather = WeatherManager.WeatherType.Storm;
        }
    }
}
