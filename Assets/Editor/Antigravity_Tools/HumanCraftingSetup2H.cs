using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class HumanCraftingSetup2H : MonoBehaviour
{
    [MenuItem("Antigravity/Setup Human 2H Axe Animation")]
    public static void Setup()
    {
        // 1. Find the 2H Chop Animation Clip
        // Path: Assets/Kevin Iglesias/Human Animations/Animations/Male/Work/Chopping/HumanM@TreeChopping01 - Loop.fbx
        string clipPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Work/Chopping/HumanM@TreeChopping01 - Loop.fbx";
        AnimationClip chopClip = null;

        // Try to load embedded clip
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(clipPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                chopClip = clip;
                break;
            }
        }

        if (!chopClip)
        {
             // Search if path fails
             string[] guids = AssetDatabase.FindAssets("TreeChopping01 - Loop t:AnimationClip");
             if (guids.Length > 0)
             {
                 chopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
             }
             // Try FBX search
             if (!chopClip)
             {
                 guids = AssetDatabase.FindAssets("HumanM@TreeChopping01 - Loop");
                 foreach(var g in guids)
                 {
                     string p = AssetDatabase.GUIDToAssetPath(g);
                     if (p.EndsWith(".fbx"))
                     {
                         Object[] searchAssets = AssetDatabase.LoadAllAssetsAtPath(p);
                         foreach (Object asset in searchAssets)
                         {
                             if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                             {
                                 chopClip = clip;
                                 goto Found;
                             }
                         }
                     }
                 }
             }
        }

        Found:
        if (!chopClip)
        {
            Debug.LogError("Could not find 'TreeChopping01 - Loop' animation!");
            return;
        }
        Debug.Log($"Found Clip: {chopClip.name}");

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
        if (!controller)
        {
             Debug.LogError("Animator is override/missing. Need native controller access.");
             return;
        }

        Undo.RecordObject(controller, "Update Harvesting State to 2H");

        // 3. Update 'Harvesting' State
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine machine = layer.stateMachine;
        
        AnimatorState harvestState = null;
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
            // Add transitions if new (should have been done by previous tool, but let's ensure)
            // AnyState -> Harvesting
             AnimatorStateTransition t = machine.AddAnyStateTransition(harvestState);
            t.AddCondition(AnimatorConditionMode.If, 0, "TriggerHarvest");
            t.duration = 0.1f;
            t.hasExitTime = false;
        }
        
        // Update Motion
        harvestState.motion = chopClip;
        Debug.Log($"Updated 'Harvesting' state with {chopClip.name}");

        // 4. Ensure Exit Transition exists
        bool hasExit = false;
        foreach(var t in harvestState.transitions)
        {
            if (t.destinationState == machine.defaultState || t.isExit) 
            {
                hasExit = true;
                // Update exit time to clip length if needed
                t.hasExitTime = true;
                t.exitTime = 0.9f; 
                t.duration = 0.25f;
            }
        }
        
        if (!hasExit && machine.defaultState != null)
        {
             var exitTrans = harvestState.AddTransition(machine.defaultState);
             exitTrans.hasExitTime = true;
             exitTrans.exitTime = 0.9f;
             exitTrans.duration = 0.25f;
        }

        Debug.Log("2H Axe Animation Setup Complete!");
    }
}
