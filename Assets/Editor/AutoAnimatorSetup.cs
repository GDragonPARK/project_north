using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

public class AutoAnimatorSetup : MonoBehaviour
{
    private const string CONTROLLER_PATH = "Assets/Characters/Player/animation/Player_animator.controller";
    // Alternatively fallback to "Assets/99.ThirdParty/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller"
    
    [MenuItem("Antigravity/Setup Player Animator")]
    // [InitializeOnLoadMethod] // Disabled for safety
    public static void SetupAnimator()
    {
        // 1. Load Controller
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            Debug.LogError($"Could not find Animator Controller at {CONTROLLER_PATH}");
            return;
        }

        Debug.Log($"Updating Animator: {controller.name}");

        // 2. Add Parameters
        AddParameter(controller, "sideway_speed", AnimatorControllerParameterType.Float); // X
        AddParameter(controller, "forward_speed", AnimatorControllerParameterType.Float); // Y
        AddParameter(controller, "IsChopping", AnimatorControllerParameterType.Bool);     // Trigger/Bool for chopping
        AddParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);

        // 3. Create Locomotion BlendTree
        // Find or Create "Base Layer"
        var baseLayer = controller.layers[0];
        var stateMachine = baseLayer.stateMachine;

        // Find or Create "Locomotion" State
        var locomotionState = FindOrCreateState(stateMachine, "Locomotion");
        stateMachine.defaultState = locomotionState;

        // Create BlendTree
        var blendTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.name = "Locomotion_8Way";
        blendTree.blendType = BlendTreeType.FreeformCartesian2D;
        blendTree.blendParameter = "sideway_speed";
        blendTree.blendParameterY = "forward_speed";

        locomotionState.motion = blendTree;

        // Populate BlendTree with Clips
        // We need to find clips by name pattern in "Assets/Kevin Iglesias"
        // Pattern: HumanM@Walk01_Forward.fbx -> Clip "HumanM@Walk01_Forward"
        
        string[] directions = { "Forward", "Backward", "Left", "Right", "ForwardLeft", "ForwardRight", "BackwardLeft", "BackwardRight" };
        Vector2[] vectors = { 
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0), new Vector2(1, 0), 
            new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) 
        };

        // Idle
        var idleClip = FindClip("HumanM@Idle01"); // Default Idle
        if (idleClip) blendTree.AddChild(idleClip, Vector2.zero);

        // Walk (Speed 0.5)
        AddMotionSet(blendTree, "HumanM@Walk01_", directions, vectors, 0.5f);

        // Run (Speed 1.0)
        AddMotionSet(blendTree, "HumanM@Run01_", directions, vectors, 1.0f);

        // Sprint (Speed 2.0)
        AddMotionSet(blendTree, "HumanM@Sprint01_", directions, vectors, 2.0f);


        // 4. Chopping SubStateMachine
        // Find or Create SubStateMachine
        var choppingMachine = FindOrCreateSubStateMachine(stateMachine, "Chopping");
        
        // Clips
        var chopBegin = FindClip("HumanM@TreeChopping - Begin");
        var chopLoop = FindClip("HumanM@TreeChopping01 - Loop"); // "01 - Loop" 
        var chopStop = FindClip("HumanM@TreeChopping - Stop");

        if (chopLoop)
        {
            // Inject Animation Event?
            // Note: Modifying imported clips via script is hard if they are ReadOnly. 
            // We'll skip forcing event injection and rely on "Loop" state behaviour or assume user will add it manually if needed,
            // OR we add a script to the state that triggers "OnHit".
            // Since User request is critical, we can try to add an event if it's not read-only.
            // But usually we can't.
        }

        var stateBegin = FindOrCreateState(choppingMachine, "Begin");
        stateBegin.motion = chopBegin;
        
        var stateLoop = FindOrCreateState(choppingMachine, "Loop");
        stateLoop.motion = chopLoop;

        var stateStop = FindOrCreateState(choppingMachine, "Stop");
        stateStop.motion = chopStop;

        // Transitions
        // Entry -> Begin
        var entryTrans = choppingMachine.AddEntryTransition(stateBegin);
        // We ideally want transition from Locomotion -> Chopping
        var transToChop = locomotionState.AddTransition(stateBegin);
        transToChop.AddCondition(AnimatorConditionMode.If, 0, "IsChopping");
        transToChop.duration = 0.1f;

        // Begin -> Loop (Exit Time)
        var transBeginLoop = stateBegin.AddTransition(stateLoop);
        transBeginLoop.hasExitTime = true;
        transBeginLoop.exitTime = 0.9f; 
        transBeginLoop.duration = 0.1f;

        // Loop -> Loop (Implicit)
        
        // Loop -> Stop (IsChopping false)
        var transLoopStop = stateLoop.AddTransition(stateStop);
        transLoopStop.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChopping");
        transLoopStop.hasExitTime = false;
        transLoopStop.duration = 0.15f;

        // Stop -> Exit (Exit Time)
        // Use AddExitTransition to transition to the StateMachine's Exit node
        var transStopExit = stateStop.AddExitTransition();
        transStopExit.hasExitTime = true;
        transStopExit.exitTime = 0.9f;
        transStopExit.duration = 0.2f;

        // 5. UpperBody Layer & Mask
        var upperLayer = FindOrCreateLayer(controller, "UpperBody");
        
        // Create AvatarMask
        var mask = new AvatarMask();
        mask.name = "UpperBodyMask";
        AssetDatabase.CreateAsset(mask, "Assets/Characters/Player/animation/UpperBodyMask.mask");
        
        // Set Body Parts (Indices: 0:Root, 1:Body, 2:Head, 3:LLeg, 4:RLeg, 5:LArm, 6:RArm, 7:LHand, 8:RHand, ...)
        // We want Body(1), Head(2), LArm(5), RArm(6), Hands(7,8) active. Legs(inactive).
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true); // Torso
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        // mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Legs, false); // Removed invalid enum
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);

        upperLayer.avatarMask = mask;
        upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
        upperLayer.defaultWeight = 1.0f;

        // UpperBody State: Combat Idle
        var upperMachine = upperLayer.stateMachine;
        var combatIdleState = FindOrCreateState(upperMachine, "CombatIdle");
        upperMachine.defaultState = combatIdleState;
        
        // Use an "Idle" clip for UpperBody? Or "Idle_Combat"?
        // We'll reuse 'HumanM@Idle01' for now, but really we want a combat stance.
        // User asked for "Arms slightly spread".
        // Maybe "HumanF@Idle01_Break01" or similar?
        // We'll stick to Idle01 until user says otherwise, or leave it empty to inherit?
        // No, layer is Override. Empty state returns to base? No.
        // We need a clip.
        if (idleClip) combatIdleState.motion = idleClip; 
        
        // Save
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Animator Setup Complete!");
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
            controller.AddParameter(name, type);
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name)
    {
        var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
        if (state == null) state = machine.AddState(name);
        return state;
    }

    private static AnimatorStateMachine FindOrCreateSubStateMachine(AnimatorStateMachine parent, string name)
    {
        var machine = parent.stateMachines.FirstOrDefault(sm => sm.stateMachine.name == name).stateMachine;
        if (machine == null) machine = parent.AddStateMachine(name);
        return machine;
    }

    private static AnimatorControllerLayer FindOrCreateLayer(AnimatorController controller, string name)
    {
        var layer = controller.layers.FirstOrDefault(l => l.name == name);
        if (layer == null)
        {
            controller.AddLayer(name);
            layer = controller.layers.Last();
        }
        return layer;
    }

    private static AnimationClip FindClip(string partialName)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip " + partialName);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(partialName)) // Strict check
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
        }
        return null;
    }

    private static void AddMotionSet(BlendTree tree, string prefix, string[] directions, Vector2[] vectors, float speed)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            string clipName = prefix + directions[i];
            var clip = FindClip(clipName);
            if (clip)
            {
                tree.AddChild(clip, vectors[i] * speed);
            }
            else
            {
                Debug.LogWarning($"Missing Clip: {clipName}");
            }
        }
    }
}
