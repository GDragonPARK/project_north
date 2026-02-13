using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacker : MonoBehaviour
{
    public Animator animator;
    public AxeInteraction axeScript; // Changed from WeaponDamageController
    public float attackCooldown = 1.0f;
    
    private float lastAttackTime;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // Auto-find AxeInteraction
        if (axeScript == null) axeScript = GetComponentInChildren<AxeInteraction>();
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time > lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
        
        if (animator) animator.SetTrigger("Attack");
        
        // Damage Window
        StartCoroutine(DamageWindowRoutine());
    }

    private System.Collections.IEnumerator DamageWindowRoutine()
    {
        // Windup
        yield return new WaitForSeconds(0.4f); // Slightly longer aligned with anim
        
        if (axeScript) axeScript.EnableHit();
        
        // Active
        yield return new WaitForSeconds(0.3f);
        
        if (axeScript) axeScript.DisableHit();
    }
}
