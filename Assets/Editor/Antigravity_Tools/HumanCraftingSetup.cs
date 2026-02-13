using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class HumanCraftingSetup : MonoBehaviour
{
    [MenuItem("Antigravity/Setup Human Crafting Animations")]
    public static void Setup()
    {
        // 1. Find the animation clip
        AnimationClip chopClip = FindClip("Chop.anim"); // Most likely KayKit/Characters/Animations/Animations/Rig_Medium/Tools/Chop.anim
        if (!chopClip)
        {
             string[] guids = AssetDatabase.FindAssets("Chop t:AnimationClip"); // Search broader
             foreach(var g in guids)
             {
                 string p = AssetDatabase.GUIDToAssetPath(g);
                 if (p.Contains("KayKit") && p.Contains("Tools")) 
                 {
                     chopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                     break;
                 }
             }
        }
        
        if (!chopClip)
        {
            Debug.LogError("Could not find 'Chop.anim' (KayKit Tools variant). Please ensure assets are imported.");
            // Fallback to any 'Chop'
             string[] guids = AssetDatabase.FindAssets("Chop t:AnimationClip");
             if (guids.Length > 0) chopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (!chopClip)
        {
            Debug.LogError("ABORT: No Chop animation found.");
            return;
        }
        Debug.Log($"Found Chop Clip: {chopClip.name} at {AssetDatabase.GetAssetPath(chopClip)}");


        // 2. Locate Player Animator
        GameObject player = GameObject.Find("Player_New");
        if (!player) 
        {
            Debug.LogError("Player_New not found.");
            return;
        }
        
        Animator anim = player.GetComponent<Animator>();
        if (!anim || !anim.runtimeAnimatorController)
        {
            Debug.LogError("Player_New has no Animator or Controller assigned.");
            return;
        }

        AnimatorController controller = anim.runtimeAnimatorController as AnimatorController;
        // If it's an override, get base? usually direct assignment for editing.
        if (!controller)
        {
             Debug.LogError("Animator is not an AnimatorController asset (might be override). Cannot edit overrides directly via script easily.");
             // Try getting the source asset path logic?
             // Usually it's just the assigned controller.
             return;
        }

        Undo.RecordObject(controller, "Add Harvesting State");

        // 3. Add Parameter
        bool hasParam = false;
        foreach(var p in controller.parameters) if (p.name == "TriggerHarvest") hasParam = true;
        if (!hasParam) controller.AddParameter("TriggerHarvest", AnimatorControllerParameterType.Trigger);

        // 4. Add State
        // Check if layer exists. Usually Base Layer is layer 0.
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine machine = layer.stateMachine;
        
        AnimatorState harvestState = null;
        // Check if state exists to avoid duplicates
        foreach(var s in machine.states) 
        {
            if (s.state.name == "Harvesting") 
            {
                harvestState = s.state;
                break;
            }
        }
        
        if (harvestState == null)
        {
            harvestState = machine.AddState("Harvesting");
            harvestState.motion = chopClip;
            Debug.Log("Created 'Harvesting' state.");
        }
        else
        {
            harvestState.motion = chopClip; // Update clip just in case
        }

        // 5. Transitions
        // Any State -> Harvesting
        bool transExists = false;
        foreach(var t in machine.anyStateTransitions)
        {
            if (t.destinationState == harvestState) 
            {
                transExists = true; 
                break;
            }
        }
        
        if (!transExists)
        {
            AnimatorStateTransition t = machine.AddAnyStateTransition(harvestState);
            t.AddCondition(AnimatorConditionMode.If, 0, "TriggerHarvest");
            t.duration = 0.1f;
            t.hasExitTime = false;
        }

        // Harvesting -> Exit (or Idle)
        // Usually back to Exit is good, or back to default state.
        // Let's connect Harvesting -> machine.defaultState
        if (machine.defaultState != null)
        {
            bool exitTransExists = false;
            foreach(var t in harvestState.transitions)
            {
                if (t.destinationState == machine.defaultState) 
                {
                    exitTransExists = true; 
                    break;
                }
            }
            
            if (!exitTransExists)
            {
                AnimatorStateTransition t = harvestState.AddTransition(machine.defaultState);
                t.hasExitTime = true; // Wait for finish
                t.exitTime = 1.0f; // End of clip
                t.duration = 0.25f; // Blend out
            }
        }

        // 6. Fix Axe Orientation
        // User asked to rotate axe 180 degrees Y or Z.
        // We can do this on the prefab or instance. Let's do instance first.
        var equipment = player.GetComponent<PlayerEquipmentManager>();
        if (equipment && equipment.axeModel)
        {
            Undo.RecordObject(equipment.axeModel.transform, "Rotate Axe Blade");
            // Standard "Hold" often makes Z forward. If blade faces self (back), it means it's 180 off.
            // Let's rotate 180 on Y local.
            // Vector3 currentRot = equipment.axeModel.transform.localEulerAngles;
            // equipment.axeModel.transform.localRotation = Quaternion.Euler(currentRot.x, currentRot.y + 180, currentRot.z);
            
            // Actually, best to reset to 0,0,0 first (done previously) then apply 180.
            equipment.axeModel.transform.localRotation = Quaternion.Euler(0, 180, 0); 
            Debug.Log("Rotated Axe 180 degrees (Y-Axis).");
        }

        Debug.Log("Human Crafting Setup Complete!");
    }

    private static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name);
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
