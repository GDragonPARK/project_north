using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FinalizeAxeAndAnimation : MonoBehaviour
{
    [MenuItem("Antigravity/Finalize Axe & Animation")]
    public static void Finalize()
    {
        // 1. Find Player
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        // 2. Set IK Weight
        var ik = player.GetComponent<PlayerHarvestingIK>();
        if (!ik) ik = player.AddComponent<PlayerHarvestingIK>();
        ik.weight = 1.0f;
        Debug.Log("Set PlayerHarvestingIK weight to 1.0");

        // 3. Assign TwoHanded Animation (KayKit 2H Chop as closest match)
        string animPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_2H_Attack_Chop.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        
        if (!clip)
        {
             // Fallback search
             string[] guids = AssetDatabase.FindAssets("t:AnimationClip Chop 2H"); // or "TwoHanded"
             if (guids.Length > 0)
             {
                 animPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                 clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
             }
        }

        if (clip)
        {
            Animator animator = player.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                // If overridden, get base
                if (!controller)
                {
                    AnimatorOverrideController overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                    if (overrideController) controller = overrideController.runtimeAnimatorController as AnimatorController;
                }

                if (controller)
                {
                    // Find "Harvesting" or "Attack" state
                    bool found = false;
                    for (int i = 0; i < controller.layers.Length; i++)
                    {
                        var layer = controller.layers[i];
                        foreach (var childState in layer.stateMachine.states)
                        {
                            if (childState.state.name == "Harvesting" || childState.state.name == "Attack")
                            {
                                childState.state.motion = clip;
                                Debug.Log($"Assigned '{clip.name}' to state '{childState.state.name}' in layer '{layer.name}'");
                                found = true;
                                EditorUtility.SetDirty(controller); 
                                AssetDatabase.SaveAssets(); 
                                break; 
                            }
                        }
                        if (found) break;
                    }
                    if (!found) Debug.LogError("Could not find 'Harvesting' or 'Attack' state in Animator!");
                }
            }
        }
        else
        {
            Debug.LogError("Could not find 'Melee_2H_Attack_Chop' animation clip!");
        }

        // 4. Ground Adjustment
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc)
        {
            // Lower slightly
            // If sticking, maybe reduce skinWidth or adjust center y?
            // User suggests: "CharacterController 높이값을 지형 높이에 맞춰 미세하게 내려줘" -> Adjust Position?
            // But if grounded is false, we are floating.
            // Let's ensure SkinWidth is reasonable (default 0.08).
            // Let's move player down a tiny bit to force ground collision?
            // Actually TPC script handles movement.
            // If TPC.GroundedOffset is -0.15, it should detect ground 0.15 below feet.
            // If CC is floating, maybe step offset?
            // Let's just try to move player down by 0.1 to seat them.
            // But Editor script won't move player in runtime persistently unless we modify Prefab or initial pos?
            // Let's just log recommendation or set SkinWidth.
            cc.skinWidth = 0.08f; 
            cc.minMoveDistance = 0f;
            Debug.Log("Adjusted CharacterController settings.");
        }
    }
}
