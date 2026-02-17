using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Piece", menuName = "ProjectNorth/Building/Piece")]
public class BuildingDataSO : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public BuildingCategorySO category;
    public GameObject prefab; // 실제 설치될 프리팹
    
    [Header("Construction Costs")]
    public List<BuildCost> constructionCosts = new List<BuildCost>();
}
