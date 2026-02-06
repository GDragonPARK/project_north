using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    [Header("Weapon Reference")]
    [Tooltip("Assign the WeaponDamageController script attached to the weapon (e.g. Axe)")]
    public WeaponDamageController weaponController;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public AudioClip[] swingClips;

    // Called by Animator Event: AnimEvent_HitOn
    public void AnimEvent_HitOn()
    {
        if (weaponController != null)
        {
            weaponController.EnableDamage();
            PlaySwingSound();
            // Debug.Log("[PlayerAnimationEvents] Damage Enabled");
        }
        else
        {
            // Warning only once or just log if debugging
            // Debug.LogWarning("[PlayerAnimationEvents] WeaponController is missing!");
        }
    }

    // Called by Animator Event: AnimEvent_HitOff
    public void AnimEvent_HitOff()
    {
        if (weaponController != null)
        {
            weaponController.DisableDamage();
            // Debug.Log("[PlayerAnimationEvents] Damage Disabled");
        }
    }

    // Called by Animator Event: FootR
    public void FootR()
    {
        PlayFootstep();
    }

    // Called by Animator Event: FootL
    public void FootL()
    {
        PlayFootstep();
    }

    private void PlayFootstep()
    {
        if (audioSource && footstepClips != null && footstepClips.Length > 0)
        {
            audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], 0.5f);
        }
    }

    private void PlaySwingSound()
    {
        if (audioSource && swingClips != null && swingClips.Length > 0)
        {
            audioSource.PlayOneShot(swingClips[Random.Range(0, swingClips.Length)]);
        }
    }
}
