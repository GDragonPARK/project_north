using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Survival/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string resultName;
    public string ingredientName;
    public int ingredientAmount;
}
