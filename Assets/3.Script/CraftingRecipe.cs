using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ResourceStack
{
    public ItemData item;
    public int amount;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Valheim/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public ItemData resultItem;
    public int resultAmount = 1;
    
    [Header("Requirements")]
    public List<ResourceStack> requiredResources;
    public bool requiresWorkbench = true;
    public int workbenchLevelRequired = 1;
}
