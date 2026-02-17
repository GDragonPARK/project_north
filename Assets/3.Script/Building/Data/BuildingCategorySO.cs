using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Category", menuName = "ProjectNorth/Building/Category")]
public class BuildingCategorySO : ScriptableObject
{
    public string categoryName;
    public List<BuildingDataSO> pieces = new List<BuildingDataSO>();
}
