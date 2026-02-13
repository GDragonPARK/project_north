using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }
    
    public GameObject interactionPanel;
    public TextMeshProUGUI interactionText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (interactionPanel) interactionPanel.SetActive(false);
    }

    public void Show(string message)
    {
        if (interactionPanel)
        {
            interactionPanel.SetActive(true);
            if (interactionText) interactionText.text = message;
        }
    }

    public void Hide()
    {
        if (interactionPanel) interactionPanel.SetActive(false);
    }
}
