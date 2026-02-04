using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class WoodcuttingRefiner : EditorWindow
{
    [MenuItem("Tools/Refine Woodcutting")]
    public static void Refine()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;

        // 1. Axe Transform Fix
        Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
        Transform axeTr = null;
        foreach (var t in allChildren) if (t.name == "axe_1handed") axeTr = t;

        if (axeTr)
        {
            axeTr.localPosition = new Vector3(0, 0.1f, 0);
            axeTr.localEulerAngles = new Vector3(0, 90, -90); // User requested (0, 90, -90)
            Debug.Log($"[Refiner] Axe Adjusted: Pos(0, 0.1, 0), Rot(0, 90, -90).");
        }

        // 2. Tree Tag Checker
        var trees = Object.FindObjectsByType<TreeFelling>(FindObjectsSortMode.None);
        foreach(var t in trees)
        {
            Debug.Log($"[Refiner] Found Tree: {t.name} | Tag: {t.tag} | Layer: {LayerMask.LayerToName(t.gameObject.layer)}");
        }

        // 3. Animation Event Fix (Slice_Horizontal)
        // Find the clip
        string[] guids = AssetDatabase.FindAssets("Melee_1H_Attack_Slice_Horizontal t:AnimationClip");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip)
            {
                AddEventsToClip(clip);
                Debug.Log($"[Refiner] Added Events to {clip.name}");
                
                // Also update Animator State to use THIS clip? User said "User changed new animation". 
                // I should check what the Animator is using.
                UpdateAnimatorToUseClip(player, clip);
            }
        }
        else
        {
             Debug.LogError("[Refiner] Could not find 'Melee_1H_Attack_Slice_Horizontal'!");
        }
        
    }

    static void AddEventsToClip(AnimationClip clip)
    {
        if (!clip) return;
        AnimationEvent[] existing = AnimationUtility.GetAnimationEvents(clip);
        bool hasStart = false; 
        bool hasEnd = false;
        foreach(var e in existing)
        {
            if(e.functionName == "AnimEvent_HitOn") hasStart = true;
            if(e.functionName == "AnimEvent_HitOff") hasEnd = true;
        }

        if(!hasStart || !hasEnd)
        {
            List<AnimationEvent> newEvents = new List<AnimationEvent>(existing);
            // Slice Horizontal might be faster?
            if(!hasStart)
            {
                AnimationEvent ev = new AnimationEvent();
                ev.functionName = "AnimEvent_HitOn";
                ev.time = clip.length * 0.2f; 
                newEvents.Add(ev);
            }
            if(!hasEnd)
            {
                AnimationEvent ev = new AnimationEvent();
                ev.functionName = "AnimEvent_HitOff";
                ev.time = clip.length * 0.6f; 
                newEvents.Add(ev);
            }
            AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
        }
    }

    static void UpdateAnimatorToUseClip(GameObject player, AnimationClip clip)
    {
        Animator anim = player.GetComponent<Animator>();
        if (!anim) return;
        AnimatorController controller = anim.runtimeAnimatorController as AnimatorController;
        if (!controller) controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/99.ThirdParty/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");
        
        if (controller)
        {
             AnimatorStateMachine rootSm = controller.layers[0].stateMachine;
             foreach(var s in rootSm.states)
             {
                 if(s.state.name == "Attack")
                 {
                     s.state.motion = clip;
                     Debug.Log($"[Refiner] Updated 'Attack' state to use {clip.name}");
                 }
             }
        }
    }
}