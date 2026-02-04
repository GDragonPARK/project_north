using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MyPlayerController m_player;
    [SerializeField] private Image m_fillImage;
    [SerializeField] private CanvasGroup m_canvasGroup;

    [Header("Settings")]
    [SerializeField] private Color m_normalColor = new Color(1f, 0.8f, 0f, 1f); // Valheim yellow
    [SerializeField] private Color m_exhaustedColor = Color.red;
    [SerializeField] private float m_fadeSpeed = 8f;

    private void Awake()
    {
        if (m_canvasGroup == null) m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup == null) m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Try to find fill image in children if not assigned
        if (m_fillImage == null)
        {
            Transform fill = transform.Find("StaminaBar_Fill");
            if (fill != null) m_fillImage = fill.GetComponent<Image>();
        }

        if (m_player == null) m_player = FindFirstObjectByType<MyPlayerController>();
    }

    private void Update()
    {
        if (m_player == null || m_fillImage == null || m_canvasGroup == null) return;

        float staminaPercent = m_player.GetStaminaNormalized();
        
        // Directly drive the fill amount
        m_fillImage.fillAmount = staminaPercent;

        // Color update
        m_fillImage.color = staminaPercent < 0.15f ? m_exhaustedColor : m_normalColor;

        // Visibility Logic (Valheim Style)
        // Show if stamina is less than max (99% check for some tolerance)
        float targetAlpha = (staminaPercent < 0.99f) ? 1f : 0f;
        
        if (Mathf.Abs(m_canvasGroup.alpha - targetAlpha) > 0.001f)
        {
            m_canvasGroup.alpha = Mathf.Lerp(m_canvasGroup.alpha, targetAlpha, Time.deltaTime * m_fadeSpeed);
        }
        else
        {
            m_canvasGroup.alpha = targetAlpha;
        }
    }
}
