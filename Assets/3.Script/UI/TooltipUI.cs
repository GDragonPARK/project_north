using UnityEngine;
using UnityEngine.UI;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("References")]
    public GameObject tooltipPanel;
    public Text itemNameText;
    public Text itemDescriptionText;
    public Text itemStatsText;

    private RectTransform m_rectTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        m_rectTransform = GetComponent<RectTransform>();
        Hide();
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            // Offset slightly from cursor
            m_rectTransform.position = mousePos + new Vector2(15, -15);
        }
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        tooltipPanel.SetActive(true);
        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.type.ToString(); // Or a custom description field

        string stats = "";
        if (item.damage > 0) stats += $"Damage: {item.damage}\n";
        if (item.staminaCost > 0) stats += $"Stamina: {item.staminaCost}\n";
        if (item.healthBonus > 0) stats += $"Health: +{item.healthBonus}\n";
        if (item.staminaBonus > 0) stats += $"Stamina: +{item.staminaBonus}\n";
        if (item.duration > 0) stats += $"Duration: {item.duration}s\n";
        
        itemStatsText.text = stats;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
}
