using UnityEngine;
using UnityEditor;

public class UrgentAxeRestore : MonoBehaviour
{
    [MenuItem("Antigravity/URGENT Restore Axe")]
    public static void Restore()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;

        var equipment = player.GetComponent<PlayerEquipmentManager>();
        if (!equipment) return;

        if (equipment.axeModel == null || equipment.axeModel.name.Contains("Pickaxe"))
        {
            Debug.LogWarning("Equipment Manager has no Axe or wrong weapon! Restoring...");
        }

        Transform rightHand = FindBone(player.transform, "hand.r", "RightHand", "Hand.R");
        if (!rightHand)
        {
            Debug.LogError("Right Hand Not Found!");
            return;
        }
        
        // 1. Load Axe Prefab
        // Try to find a good axe
        // valheim_Data\GameElements\Items\weapons\AxeIron.prefab or similar
        // Let's try "valheim_Data/GameElements/Items/weapons/AxeBlackMetal.prefab" or just find one
        
        GameObject newAxe = null;
        
        string[] searchTerms = new string[] { "AxeIron", "AxeBlackMetal", "AxeFlint", "AxeStone", "AxeWood" };
        
        foreach (var term in searchTerms)
        {
            string[] guids = AssetDatabase.FindAssets(term + " t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab)
                {
                    newAxe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rightHand);
                    newAxe.name = "Restored_" + term;
                    Debug.Log($"Restored {term} from {path}");
                    break;
                }
            }
        }

        if (newAxe == null)
        {
            Debug.LogError("Could not find any Axe Prefab (Iron, BlackMetal, Flint, Stone, Wood) to restore!");
            return;
        }
        
        // 3. Reset Transform
        // User wanted blade OUT. 
        // Start with (0,0,0) and (0, 180, 0) logic from before
        newAxe.transform.localPosition = Vector3.zero;
        // 3. Reset Transform
        newAxe.transform.localPosition = Vector3.zero;
        newAxe.transform.localRotation = Quaternion.Euler(0, 180, 0); 
        newAxe.transform.localScale = Vector3.one;

        // 4. Assign to Manager
        Undo.RecordObject(equipment, "Assign Axe");
        equipment.axeModel = newAxe;
        
        // Ensure Pickaxe is not conflicting
        if (equipment.pickaxeModel != null) equipment.pickaxeModel.SetActive(false);
        
        // Force Equip Axe
        equipment.EquipAxe();

        Debug.Log($"Restored Axe: {newAxe.name} to {rightHand.name} and assigned to Manager.");
    }
    
    private static Transform FindBone(Transform t, params string[] names)
    {
        foreach(var n in names)
        {
            if (t.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return t;
        }
        foreach(Transform child in t)
        {
            var res = FindBone(child, names);
            if (res) return res;
        }
        return null;
    }
}
