using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuildCatalog", menuName = "Building/Catalog")]
public class BuildCatalogSO : ScriptableObject
{
    public List<BuildRecipeSO> recipes = new List<BuildRecipeSO>();
}
