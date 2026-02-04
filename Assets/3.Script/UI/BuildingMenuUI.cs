using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingMenuUI : MonoBehaviour
{
    [Header("References")]
    public BuildManager buildManager;
    public GameObject menuPanel;
    public Transform contentParent;
    public GameObject buttonPrefab;

    private bool m_isMenuOpen = false;

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        PopulateMenu();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        m_isMenuOpen = !m_isMenuOpen;
        menuPanel.SetActive(m_isMenuOpen);

        if (m_isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PopulateMenu()
    {
        if (buildManager == null || buttonPrefab == null || contentParent == null) return;

        // Clear existing buttons
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Create buttons for each buildable piece
        for (int i = 0; i < buildManager.buildablePieces.Count; i++)
        {
            int index = i;
            ItemData data = buildManager.buildablePieces[i];
            
            GameObject btnObj = Instantiate(buttonPrefab, contentParent);
            btnObj.name = $"Btn_{data.itemName}";

            // Setup button text/icon
            Text txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = data.itemName;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => {
                    buildManager.SelectPiece(index);
                    ToggleMenu();
                });
            }
        }
    }
}
