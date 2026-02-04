using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Data")]
    public List<CraftingRecipe> allRecipes;
    
    [Header("Player Reference")]
    public Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null) return false;

        // 1. Check Workbench Requirement
        if (recipe.requiresWorkbench)
        {
            if (!Workbench.IsInRange(playerTransform.position, out Workbench wb))
            {
                // Debug.Log("Too far from a workbench!");
                return false;
            }
            if (wb.level < recipe.workbenchLevelRequired)
            {
                // Debug.Log("Workbench level too low!");
                return false;
            }
        }

        // 2. Check Resources
        foreach (var req in recipe.requiredResources)
        {
            if (!InventorySystem.Instance.HasItem(req.item, req.amount))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsCrafting { get; private set; }
    public float CraftProgress { get; private set; }

    public void CraftItem(CraftingRecipe recipe)
    {
        if (CanCraft(recipe) && !IsCrafting)
        {
            StartCoroutine(CraftRoutine(recipe));
        }
    }

    private System.Collections.IEnumerator CraftRoutine(CraftingRecipe recipe)
    {
        IsCrafting = true;
        CraftProgress = 0f;
        float craftDuration = 1.5f;

        while (CraftProgress < 1f)
        {
            CraftProgress += Time.deltaTime / craftDuration;
            yield return null;
        }

        // Consume resources
        foreach (var req in recipe.requiredResources)
        {
            InventorySystem.Instance.RemoveItem(req.item, req.amount);
        }

        // Add resulting item
        InventorySystem.Instance.AddItem(recipe.resultItem, recipe.resultAmount);
        
        Debug.Log($"<color=cyan>Crafted: {recipe.resultAmount}x {recipe.resultItem.itemName}</color>");
        
        IsCrafting = false;
        CraftProgress = 0f;
    }
}
