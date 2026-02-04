using UnityEngine;
using UnityEditor;

public class AnimationInspector : EditorWindow
{
    [MenuItem("Tools/Inspect Animator States")]
    public static void Inspect()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player) return;

        Animator anim = player.GetComponent<Animator>();
        if (!anim || !anim.runtimeAnimatorController) return;

        // Log current Idle clip
        // Usually in a BlendTree called "IdleWalkRun" or State "Idle"
        // Need to traverse AnimatorController
        // Not easy via Runtime, better via AssetDatabase if we knew the path.
        // But we can try to find the "Idle" clip usage.
        
        Debug.Log("--- Animation Inspector ---");
        foreach(var clip in anim.runtimeAnimatorController.animationClips)
        {
            if(clip.name.Contains("Idle"))
            {
                Debug.Log($"Used Clip: {clip.name}");
            }
        }
    }
}