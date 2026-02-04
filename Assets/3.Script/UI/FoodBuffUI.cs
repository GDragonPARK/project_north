using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FoodBuffUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject m_buffIconPrefab;
    [SerializeField] private Transform m_iconParent;

    private List<GameObject> m_activeIcons = new List<GameObject>();

    private void Start()
    {
        if (FoodSystem.Instance != null)
        {
            FoodSystem.Instance.OnFoodChanged += RefreshUI;
        }
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (FoodSystem.Instance != null)
        {
            FoodSystem.Instance.OnFoodChanged -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        foreach (var icon in m_activeIcons) Destroy(icon);
        m_activeIcons.Clear();

        if (FoodSystem.Instance == null) return;

        foreach (var food in FoodSystem.Instance.GetActiveFoods())
        {
            GameObject iconObj = Instantiate(m_buffIconPrefab, m_iconParent);
            m_activeIcons.Add(iconObj);

            // Setup icon image
            Image img = iconObj.GetComponentInChildren<Image>();
            if (img != null && food.data.icon != null) img.sprite = food.data.icon;

            // Setup duration overlay (circular fill or local scale)
            // We can add a script to the prefab to update this per frame
            BuffIcon script = iconObj.AddComponent<BuffIcon>();
            script.food = food;
        }
    }
}

public class BuffIcon : MonoBehaviour
{
    public FoodSystem.ActiveFood food;
    private Image m_fillImage; // Assumes a progress circle

    private void Start()
    {
        m_fillImage = GetComponent<Image>(); // Or find nested
    }

    private void Update()
    {
        if (food == null) return;
        
        // Simple scale or fill update to show degradation
        float ratio = food.remainingTime / food.totalDuration;
        transform.localScale = Vector3.one * (0.5f + 0.5f * ratio);
        
        // If there's a specialized fill image
        if (m_fillImage != null) m_fillImage.fillAmount = ratio;
    }
}
