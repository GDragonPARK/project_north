using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject craftingPanel;
    public Transform recipeListParent;
    public GameObject recipeButtonPrefab;
    
    [Header("Details Panel")]
    public Text selectedRecipeName;
    public Text requirementsText;
    public Button craftButton;

    private CraftingRecipe m_selectedRecipe;

    [Header("Crafting Animation")]
    public Slider craftingProgressBar;

    private void Start()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (craftingProgressBar != null) craftingProgressBar.gameObject.SetActive(false);
        RefreshRecipeList();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleUI();
        }

        UpdateCraftingAnimation();
    }

    private void UpdateCraftingAnimation()
    {
        if (CraftingManager.Instance == null || craftingProgressBar == null) return;

        bool crafting = CraftingManager.Instance.IsCrafting;
        craftingProgressBar.gameObject.SetActive(crafting);
        
        if (crafting)
        {
            craftingProgressBar.value = CraftingManager.Instance.CraftProgress;
            craftButton.interactable = false;
        }
    }

    public void ToggleUI()
    {
        bool isOpen = !craftingPanel.activeSelf;
        craftingPanel.SetActive(isOpen);
        
        if (isOpen)
        {
            RefreshRecipeList();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RefreshRecipeList()
    {
        if (CraftingManager.Instance == null || recipeButtonPrefab == null) return;

        foreach (Transform child in recipeListParent) Destroy(child.gameObject);

        foreach (var recipe in CraftingManager.Instance.allRecipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListParent);
            btnObj.GetComponentInChildren<Text>().text = recipe.recipeName;
            
            // Add Tooltip Support
            TooltipTrigger trigger = btnObj.AddComponent<TooltipTrigger>();
            trigger.item = recipe.resultItem;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectRecipe(recipe));
            
            // Visual indicating if craftable
            btn.interactable = CraftingManager.Instance.CanCraft(recipe);
        }
    }

    public void SelectRecipe(CraftingRecipe recipe)
    {
        m_selectedRecipe = recipe;
        selectedRecipeName.text = recipe.recipeName;

        string reqStr = "";
        foreach (var req in recipe.requiredResources)
        {
            int current = InventorySystem.Instance.items.ContainsKey(req.item) ? InventorySystem.Instance.items[req.item] : 0;
            string color = current >= req.amount ? "white" : "red";
            reqStr += $"<color={color}>{req.item.itemName}: {current}/{req.amount}</color>\n";
        }
        
        if (recipe.requiresWorkbench)
        {
            bool inRange = Workbench.IsInRange(CraftingManager.Instance.playerTransform.position, out _);
            string color = inRange ? "white" : "red";
            reqStr += $"<color={color}>(Requires Workbench)</color>";
        }

        requirementsText.text = reqStr;
        craftButton.interactable = CraftingManager.Instance.CanCraft(recipe);
    }

    public void OnCraftButtonClicked()
    {
        if (m_selectedRecipe != null)
        {
            CraftingManager.Instance.CraftItem(m_selectedRecipe);
            SelectRecipe(m_selectedRecipe); // Refresh text
            RefreshRecipeList(); // Refresh list icons/states
        }
    }
}
