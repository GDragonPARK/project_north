using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class IdleSwapper : EditorWindow
{
    [MenuItem("Tools/Swap Idle Animation")]
    public static void Swap()
    {
        GameObject player = GameObject.Find("Player_New");
        if(!player) return;
        
        Animator anim = player.GetComponent<Animator>();
        if(!anim) return;
        
        // Find New Idle Clip
        // Path from Step 744: Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_Unarmed_Idle.anim
        // Full path: Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_Unarmed_Idle.anim
        string clipPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_Unarmed_Idle.anim";
        AnimationClip newIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        
        if(!newIdle)
        {
            Debug.LogError($"Could not load idle clip at {clipPath}");
            // Try Find?
            string[] guids = AssetDatabase.FindAssets("Melee_Unarmed_Idle t:AnimationClip");
            if(guids.Length > 0) newIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        
        if(!newIdle) { Debug.LogError("Idle Clip not found!"); return; }
        
        // Update Controller
        AnimatorController controller = anim.runtimeAnimatorController as AnimatorController;
        // Search Layers
        foreach(var layer in controller.layers)
        {
            SwapInStateMachine(layer.stateMachine, newIdle);
        }
        
        Debug.Log($"Swapped Idle Animation to {newIdle.name}");
    }
    
    static void SwapInStateMachine(AnimatorStateMachine sm, AnimationClip newClip)
    {
        // 1. Check States
        foreach(var state in sm.states)
        {
            if(state.state.name.Contains("Idle") && state.state.motion is AnimationClip)
            {
                 // Replace direct State
                 state.state.motion = newClip;
                 Debug.Log($"Updated State {state.state.name} to use {newClip.name}");
            }
            else if(state.state.motion is BlendTree bt)
            {
                SwapInBlendTree(bt, newClip);
            }
        }
        
        // 2. Check SubSMs
        foreach(var sub in sm.stateMachines) SwapInStateMachine(sub.stateMachine, newClip);
    }
    
    static void SwapInBlendTree(BlendTree bt, AnimationClip newClip)
    {
        // StarterAssets usually has "IdleWalkRun" BlendTree.
        // Child 0 is usually Idle (Speed = 0).
        
        ChildMotion[] children = bt.children;
        for(int i=0; i<children.Length; ++i)
        {
            // Heuristic: If threshold is 0, it's Idle.
            // Or if name contains Idle.
            if(children[i].threshold < 0.1f)
            {
                 children[i].motion = newClip; // Struct copy?
                 // BlendTree children array is weird. Need to reassign array or modify internal object?
                 // Modifying the array element modifies the struct but not the Tree?
                 // "bt.children returns a copy".
            }
        }
        bt.children = children; // Apply back
        
        // Recurse
        foreach(var child in children)
        {
            if(child.motion is BlendTree subBt) SwapInBlendTree(subBt, newClip);
        }
    }
}