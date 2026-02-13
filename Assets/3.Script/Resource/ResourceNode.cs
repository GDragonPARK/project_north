using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visuals")]
    public GameObject hitParticlePrefab; // Wood particles
    public Transform visualModel; // The mesh to wobble
    public float wobbleIntensity = 5f;
    
    [Header("Felling")]
    public GameObject fallingTreePrefab; // The Rigidbody version
    public float fallForce = 500f; // Force to apply when spawning
    
    // Loot
    public GameObject lootPrefab; // Wood Item to spawn
    public int lootAmount = 3;
    
    // Internal
    private bool isDead = false;
    private Quaternion originalRot;
    private Coroutine wobbleRoutine;

    private void Start()
    {
        currentHealth = maxHealth;
        if (visualModel == null) visualModel = transform;
        originalRot = visualModel.localRotation;
    }

    public void GetHit(float damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        // Visual Feedback
        if (wobbleRoutine != null) StopCoroutine(wobbleRoutine);
        wobbleRoutine = StartCoroutine(WobbleRoutine(hitDir));
        
        SpawnParticles(hitPoint, hitDir);

        if (currentHealth <= 0)
        {
            Die(hitDir);
        }
    }

    private IEnumerator WobbleRoutine(Vector3 hitDir)
    {
        float elapsed = 0f;
        float duration = 0.4f;
        
        // Axis of rotation is perpendicular to hit direction
        Vector3 axis = Vector3.Cross(Vector3.up, hitDir).normalized;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Damped sine wave
            float angle = Mathf.Sin(t * Mathf.PI * 4f) * wobbleIntensity * (1f - t);
            visualModel.localRotation = originalRot * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }
        visualModel.localRotation = originalRot;
    }

    private void SpawnParticles(Vector3 pos, Vector3 dir)
    {
        if (hitParticlePrefab)
        {
             GameObject p = Instantiate(hitParticlePrefab, pos, Quaternion.LookRotation(dir));
             Destroy(p, 2f);
        }
    }

    private void Die(Vector3 hitDir)
    {
        isDead = true;
        
        GameObject fallingObj = null;

        // Spawn Falling Tree
        if (fallingTreePrefab)
        {
            fallingObj = Instantiate(fallingTreePrefab, transform.position, transform.rotation);
            fallingObj.transform.localScale = transform.localScale;
            
            // Cleanup: Remove ResourceNode from the falling instance to prevent recursion
            ResourceNode resNode = fallingObj.GetComponent<ResourceNode>();
            if (resNode) Destroy(resNode);

            // Cleanup: Remove static Trigger Collider
            BoxCollider box = fallingObj.GetComponent<BoxCollider>();
            if (box) Destroy(box);
            
            // Physics: Add Rigidbody
            Rigidbody rb = fallingObj.GetComponent<Rigidbody>();
            if (!rb) rb = fallingObj.AddComponent<Rigidbody>();
            rb.mass = 200f; 
            
            // Physics: Add Capsule Collider for rolling
            CapsuleCollider cap = fallingObj.GetComponent<CapsuleCollider>();
            if (!cap) 
            {
                cap = fallingObj.AddComponent<CapsuleCollider>();
                cap.radius = 0.5f;
                cap.height = 5f;
                cap.direction = 1; // Y-Axis
                cap.center = new Vector3(0, 2.5f, 0);
            }

            // FIX: Remove or Fix MeshColliders to prevent "Concave MeshCollider with non-kinematic RB" error
            MeshCollider[] meshCols = fallingObj.GetComponentsInChildren<MeshCollider>();
            foreach(var mc in meshCols)
            {
                // Option A: Make convex (might act weird for complex trees)
                // mc.convex = true; 
                
                // Option B: Destroy them and rely on the Capsule we just added (Safest for falling logs)
                Destroy(mc); 
            }

            // Push it!
            Vector3 pushDir = new Vector3(hitDir.x, 0, hitDir.z).normalized;
            rb.AddForce(pushDir * fallForce, ForceMode.Impulse);
            
            // Apply FallenLog script for loot
            FallenLog logScript = fallingObj.GetComponent<FallenLog>();
            if (!logScript) logScript = fallingObj.AddComponent<FallenLog>();
            
            // Pass loot info
            logScript.lootPrefab = lootPrefab;
            logScript.lootAmount = lootAmount;
        }

        // Disable static tree
        gameObject.SetActive(false); 
    }
}
