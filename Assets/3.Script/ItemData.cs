using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Valheim/Item Data")]
[System.Serializable]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    
    public enum ItemType 
    { 
        Default, 
        Food, 
        Weapon, 
        Armor, 
        Resource, 
        Tool, 
        Misc 
    }
    public ItemType type;

    [Header("Consumable Stats")]
    public int healthBonus;
    public float staminaBonus;
    public float duration;

    [Header("Combat & Tool Stats")]
    public float damage;
    public float staminaCost;
    public float attackRange = 2f;
    public int woodCost = 0; // Cost for building (used by BuildManager)
    
    [Header("Prefabs")]
    public GameObject itemPrefab; // Used for dropping/building?
    public GameObject weaponPrefab; // Used for equipping visuals
}
