#if FALSE
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using StarterAssets;
using System.Collections.Generic;

public class WoodcuttingSetup : EditorWindow
{
    [MenuItem("Tools/Setup Woodcutting System")]
    public static void Setup()
    {
        Debug.Log("--- Starting Woodcutting Setup ---"); 

        GameObject player = GameObject.Find("Player_New");
        if (!player) { Debug.LogError("Player_New not found!"); return; }

        // 1. Setup Axe (Rotation, Collider, Script)
        SetupAxe(player);

        // 2. Setup Trees (Add TreeFelling)
        SetupTrees();

        // 3. Setup Animator (Attack State, Events, Parameters)
        SetupAnimator(player);

        Debug.Log("--- Woodcutting Setup Complete ---");
    }

    static void SetupAxe(GameObject player)
    {
        Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
        Transform axeTr = null;
        foreach (var t in allChildren) if (t.name == "axe_1handed") { axeTr = t; break; }

        if (axeTr)
        {
            GameObject axe = axeTr.gameObject;
            
            // Visuals
            axe.transform.localEulerAngles = new Vector3(180, 0, 0); // Blade Down
            Debug.Log("Axe: Rotated to (180,0,0)");

            // Components
            var bc = axe.GetComponent<BoxCollider>();
            if (!bc) bc = axe.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = new Vector3(0, 0.5f, 0); 
            bc.size = new Vector3(0.3f, 1.2f, 0.3f);

            var wdc = axe.GetComponent<WeaponDamageController>();
            if (!wdc) wdc = axe.AddComponent<WeaponDamageController>();

            // Link to Player
            var tpc = player.GetComponent<ThirdPersonController>();
            if (tpc)
            {
                tpc.CurrentWeapon = wdc;
                Debug.Log("Axe: Linked to ThirdPersonController");
            }
        }
        else
        {
            Debug.LogError("Axe 'axe_1handed' not found!");
        }
    }

    static void SetupTrees()
    {
        // Find Environment_Manager children?
        GameObject env = GameObject.Find("Environment_Manager");
        if (env)
        {
            int count = 0;
            // Iterate all children. Recursive?
            // Assuming flat or VegetationSpawner structure
            foreach (Transform t in env.transform)
            {
                if (t.name.Contains("Tree") || t.name.Contains("Birch"))
                {
                    if (!t.GetComponent<TreeFelling>())
                    {
                        t.gameObject.AddComponent<TreeFelling>();
                        count++;
                    }
                    // Ensure Tag? User didn't specify, but Weapon checks Component.
                    // Ensure Layers? Default is usually fine, but 'Ground' layer might be needed for TPC collision?
                    // Trees should be obstacles. Layer 0 (Default) is fine for Weapon Trigger.
                }
            }
            Debug.Log($"Trees: Added TreeFelling to {count} trees.");
        }
    }

    static void SetupAnimator(GameObject player)
    {
        Animator anim = player.GetComponent<Animator>();
        if (!anim) return;

        AnimatorController controller = anim.runtimeAnimatorController as AnimatorController;
        if (!controller)
        {
            // Maybe it's an Override?
            Debug.LogWarning("Animator is not using a raw AnimatorController (maybe Override?) - Trying to load asset directly");
            // Hardcode path as fallback
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/99.ThirdParty/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");
        }

        if (controller)
        {
            // 1. Parameter
            bool hasParam = false;
            foreach (var p in controller.parameters) if (p.name == "Attack") hasParam = true;
            if (!hasParam) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            // 2. State & Transition
            // Find logic layer (usually Base Layer)
            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;
            
            // Allow looking for existing state
            AnimatorState attackState = null;
            foreach(var s in rootSm.states) if(s.state.name == "Attack") attackState = s.state;

            if (attackState == null)
            {
                attackState = rootSm.AddState("Attack");
                attackState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_1H_Attack_Chop.anim"); // Path?
                // Note: The path found in search was "Characters/Animations/Animations/..." 
                // Full path check: "Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_1H_Attack_Chop.anim" ?
                // Search result: "Characters\Animations\Animations\Rig_Medium\Combat Melee\Melee_1H_Attack_Chop.anim" (Relative to Assets?) 
                // Wait. Search result output usually relative to search dir? No, relative to Assets folder usually or full relative.
                // Step 536 output: "Characters\Animations\Animations\Rig_Medium\Combat Melee\Melee_1H_Attack_Chop.anim"
                // My search dir was `...Assets\valheim_Data\Prefabs\KayKit`.
                // So Full path: `Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_1H_Attack_Chop.anim`.
                // I'll try to FindAsset to be safe.
                string[] guids = AssetDatabase.FindAssets("Free Low Poly Nature Pack/Prefabs/PP_Birch_Tree_05"); // No...
                guids = AssetDatabase.FindAssets("Melee_1H_Attack_Chop t:AnimationClip");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    attackState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    Debug.Log($"Animator: Linked Attack Clip from {path}");
                    
                    // 3. Add Events to Clip!
                    AddEventsToClip(attackState.motion as AnimationClip);
                }
                else
                {
                    Debug.LogError("Animator: Could not find 'Melee_1H_Attack_Chop' animation!");
                }
                
                // Transitions
                // Any State -> Attack? Or Idle -> Attack.
                // Any State is responsive.
                AnimatorStateTransition tr = rootSm.AddAnyStateTransition(attackState);
                tr.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                tr.duration = 0.1f;
                
                // Exit
                AnimatorStateTransition exit = attackState.AddExitTransition();
                exit.hasExitTime = true;
                exit.exitTime = 0.9f;
                exit.duration = 0.25f;
            }
            Debug.Log("Animator: Attack State Configured.");
        }
    }

    static void AddEventsToClip(AnimationClip clip)
    {
        if (!clip) return;
        
        // Avoid duplicates
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

            if(!hasStart)
            {
                AnimationEvent ev = new AnimationEvent();
                ev.functionName = "AnimEvent_HitOn";
                ev.time = clip.length * 0.3f; // 30% mark
                newEvents.Add(ev);
            }
            if(!hasEnd)
            {
                AnimationEvent ev = new AnimationEvent();
                ev.functionName = "AnimEvent_HitOff";
                ev.time = clip.length * 0.6f; // 60% mark
                newEvents.Add(ev);
            }
            
            AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
            Debug.Log($"Animator: Added Hit Events to {clip.name}");
        }
    }
}
#endif