using UnityEngine;
using System.Collections.Generic;

public class FoodSystem : MonoBehaviour
{
    public static FoodSystem Instance { get; private set; }

    [System.Serializable]
    public class ActiveFood
    {
        public ItemData data;
        public float remainingTime;
        public float totalDuration;

        public ActiveFood(ItemData data)
        {
            this.data = data;
            this.totalDuration = data.duration;
            this.remainingTime = data.duration;
        }

        // Valheim food degrades over time
        public float GetCurrentHealthBonus()
        {
            float ratio = remainingTime / totalDuration;
            return data.healthBonus * ratio;
        }

        public float GetCurrentStaminaBonus()
        {
            float ratio = remainingTime / totalDuration;
            return data.staminaBonus * ratio;
        }
    }

    [Header("Settings")]
    public int maxFoodSlots = 3;
    private List<ActiveFood> m_activeFoods = new List<ActiveFood>();

    public System.Action OnFoodChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        bool changed = false;
        for (int i = m_activeFoods.Count - 1; i >= 0; i--)
        {
            m_activeFoods[i].remainingTime -= Time.deltaTime;
            if (m_activeFoods[i].remainingTime <= 0)
            {
                m_activeFoods.RemoveAt(i);
                changed = true;
            }
        }

        if (changed) OnFoodChanged?.Invoke();
    }

    public bool CanEat(ItemData food)
    {
        if (food == null || food.type != ItemData.ItemType.Food) return false;
        if (m_activeFoods.Count >= maxFoodSlots) return false;

        // Check for duplicates
        foreach (var active in m_activeFoods)
        {
            if (active.data == food) return false;
        }

        return true;
    }

    public void Eat(ItemData food)
    {
        if (!CanEat(food)) return;

        m_activeFoods.Add(new ActiveFood(food));
        OnFoodChanged?.Invoke();
        Debug.Log($"<color=orange>Ate {food.itemName}!</color>");
    }

    public float GetTotalHealthBonus()
    {
        float total = 0;
        foreach (var food in m_activeFoods) total += food.GetCurrentHealthBonus();
        return total;
    }

    public float GetTotalStaminaBonus()
    {
        float total = 0;
        foreach (var food in m_activeFoods) total += food.GetCurrentStaminaBonus();
        return total;
    }

    public List<ActiveFood> GetActiveFoods() => m_activeFoods;
}
