using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildCost
{
    public ItemData item;
    public int amount;
}

[CreateAssetMenu(fileName = "NewBuildRecipe", menuName = "Building/Recipe")]
public class BuildRecipeSO : ScriptableObject
{
    public string pieceName;
    public Sprite icon;
    public GameObject prefab;
    public List<BuildCost> costs;
    
    [Header("Preview Settings")]
    public GameObject previewPrefab; // Optional: If different from main prefab logic
    
    [Header("Category")]
    public string category = "Misc";
}
