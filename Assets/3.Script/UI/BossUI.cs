using UnityEngine;
using UnityEngine.UI;

public class BossUI : MonoBehaviour
{
    public static BossUI Instance { get; private set; }

    [Header("References")]
    public GameObject uiRoot;
    public Text bossNameText;
    public Slider healthSlider;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (uiRoot != null) uiRoot.SetActive(false);
    }

    public void ShowBossUI(BossAI boss)
    {
        if (uiRoot == null) return;
        
        uiRoot.SetActive(true);
        bossNameText.text = boss.bossName;
        UpdateHealth(boss.maxHealth, boss.maxHealth);

        boss.OnHealthChanged += UpdateHealth;
        boss.OnBossDeath += HideBossUI;
    }

    public void UpdateHealth(float current, float max)
    {
        if (healthSlider != null) healthSlider.value = current / max;
    }

    public void HideBossUI()
    {
        if (uiRoot != null) uiRoot.SetActive(false);
    }
}
