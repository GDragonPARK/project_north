using UnityEngine;
using UnityEditor;

public class EquipTwoHandedAxe : MonoBehaviour
{
    [MenuItem("Antigravity/Equip 2-Handed Axe")]
    public static void Equip()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player_New not found.");
            return;
        }

        PlayerEquipmentManager equipment = player.GetComponent<PlayerEquipmentManager>();
        if (!equipment)
        {
            Debug.LogError("PlayerEquipmentManager not found.");
            return;
        }

        // 1. Find the 2-Handed Axe Prefab
        // Path known from search: valheim_Data\Prefabs\KayKit\Characters\KayKit - Adventurers (for Unity)\Prefabs\Accessories\axe_2handed.prefab
        // Or search by name to be safe
        GameObject axePrefab = null;
        string[] guids = AssetDatabase.FindAssets("axe_2handed t:Prefab");
        foreach(var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith("axe_2handed.prefab"))
            {
                axePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                break;
            }
        }

        if (!axePrefab)
        {
            Debug.LogError("Could not find 'axe_2handed.prefab'.");
            return;
        }

        // 2. Find Right Hand
        Transform rightHand = FindBone(player.transform, "hand.r", "RightHand", "Hand.R");
        if (!rightHand)
        {
            Debug.LogError("Right Hand bone not found.");
            return;
        }

        // 3. Destroy old axe (if any)
        if (equipment.axeModel)
        {
            Undo.DestroyObjectImmediate(equipment.axeModel);
        }
        
        // Also clean up any 'Restored_' or 'Axe' children in hand just in case
        var children = new System.Collections.Generic.List<GameObject>();
        foreach(Transform child in rightHand) children.Add(child.gameObject);
        
        foreach(var child in children)
        {
             if (child.name.Contains("Axe") || child.name.Contains("axe"))
             {
                 Undo.DestroyObjectImmediate(child);
             }
        }

        // 4. Instantiate New Axe
        GameObject newAxe = PrefabUtility.InstantiatePrefab(axePrefab, rightHand) as GameObject;
        newAxe.name = "Axe_2Handed";
        
        // 5. Position & Rotate
        // User wants blade OUT. 
        // 2-Handed Axes might need different rotation.
        // Let's try standard align (0,0,0) position.
        // For Rotation: Try (0, 180, 0) as requested before, or (0, 90, 0).
        // Since it's a new model, let's start with Identity then rotate 180 Y if needed.
        // User said previous 180-fix worked for direction but model was invisible/particle. 
        // Let's apply (0, 180, 0) as a starting point.
        newAxe.transform.localPosition = Vector3.zero;
        newAxe.transform.localRotation = Quaternion.Euler(0, 180, 0); 
        newAxe.transform.localScale = Vector3.one;

        // 6. Assign to Manager
        Undo.RecordObject(equipment, "Equip 2H Axe");
        equipment.axeModel = newAxe;
        equipment.EquipAxe();

        Debug.Log($"Equipped 2-Handed Axe: {newAxe.name} to {rightHand.name}");
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
