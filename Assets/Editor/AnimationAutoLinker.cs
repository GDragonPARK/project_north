using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Antigravity.Editor
{
    public class AnimationAutoLinker : EditorWindow
    {
        [MenuItem("Tools/Auto Link Animations")]
        public static void LinkAnimations()
        {
            // 1. Find the Player and Animator
            GameObject player = GameObject.Find("Player") ?? GameObject.Find("PlayerArmature");
            if (!player) 
            {
                Debug.LogError("Player not found!");
                return;
            }

            Animator animator = player.GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("Player has no Animator!");
                return;
            }

            // 2. Clone Controller to avoid modifying the original Starter Asset
            RuntimeAnimatorController srcController = animator.runtimeAnimatorController;
            if (srcController == null)
            {
                Debug.LogError("Animator has no Controller assigned!");
                return;
            }

            string path = AssetDatabase.GetAssetPath(srcController);
            // If it's already a custom override/clone, keep it. 
            // Better to just create an OverrideController for safety.
            
            AnimatorOverrideController overrideController = new AnimatorOverrideController(srcController);
            string newPath = "Assets/Settings/PlayerAnimatorOverride.overrideController";
            AssetDatabase.CreateAsset(overrideController, newPath);
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"Created Animator Override Controller at {newPath}");

            // 3. Find Attack Animations
            // Search for "Slash", "Attack", "Swing" in KayKit folder
            // Path: Assets/valheim_Data/Prefabs/KayKit/Characters/Animations/Animations/Rig_Large (or Medium)
            string searchPath = "Assets/valheim_Data/Prefabs/KayKit/Characters/Animations";
            string[] guids = AssetDatabase.FindAssets("Attack t:AnimationClip", new[] { searchPath });
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("Slash t:AnimationClip", new[] { searchPath });
             
             // If still nothing, GLOBAL search
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("Attack t:AnimationClip");

            AnimationClip attackClip = null;
            if (guids.Length > 0)
            {
                 attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
                 Debug.Log($"Found Attack Clip: {attackClip.name}");
            }

            // 4. Override "Attack" state if it exists, or just log.
            // Since we don't know the exact state names in the StarterAssets controller without parsing it,
            // we will try to find a clip named "Punch" or "Attack" to replace.
            
            // StarterAssets usually has "Walk", "Run", "Jump". Attack might not be there.
            // We'll rely on our MyPlayerController trigger "Attack". The Animator needs a State "Attack".
            // If the base controller doesn't have "Attack", an Override won't help unless we edit the base controller too (add state).
            // For this task, let's assume valid setup or we create a new Controller logic.
            
            // Actually, simplest is to just output the clip we found, and let the user (or next tool) hook it up 
            // if we can't cleanly modify the state machine via API easily without "Unity.AnimatorController" access.
            
            if (attackClip)
            {
                // Inject logic: We'll modify the OverrideController 'Clips' list
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);
                
                // Try to find a dummy 'Punch' or 'Attack' clip in original to replace
                bool replaced = false;
                foreach(var pair in overrides)
                {
                    if (pair.Key.name.Contains("Punch") || pair.Key.name.Contains("Attack"))
                    {
                        overrides.Remove(pair);
                        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, attackClip));
                        replaced = true;
                        break;
                    }
                }
                
                if (replaced)
                {
                    overrideController.ApplyOverrides(overrides);
                    Debug.Log($"Replaced attack animation with {attackClip.name}");
                }
                else
                {
                    Debug.LogWarning("Could not find an existing 'Attack' or 'Punch' slot in the controller to override. You may need to add an Attack state to the Animator Window manually.");
                }
            }

            // 5. Retargeting Check
            // Ensure the Knight Avatar is Humanoid (Already handled in PlayerSystemUpgrader)
            if (animator.avatar && !animator.avatar.isHuman)
            {
                Debug.LogError("Current Avatar is NOT Humanoid! Animations will not play correctly.");
            }
            else
            {
                Debug.Log($"Avatar {animator.avatar.name} is Humanoid. Retargeting should work automatically.");
            }
        }
    }
}
