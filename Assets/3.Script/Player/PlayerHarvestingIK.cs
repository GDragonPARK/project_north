using UnityEngine;

public class PlayerHarvestingIK : MonoBehaviour
{
    [Header("IK Settings")]
    public Transform leftHandObj; // The target attached to the Axe
    [Range(0, 1)] public float weight = 1.0f;
    
    private Animator anim;
    
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!anim) return;
        
        // Auto-find logic:
        // If leftHandObj not set, look on the active Axe
        if (leftHandObj == null)
        {
             // Try to find it on axe_2Handed or current axe
             var equipment = GetComponent<PlayerEquipmentManager>();
             if (equipment && equipment.axeModel && equipment.axeModel.activeInHierarchy)
             {
                 // Look for child named "LeftHandGrip"
                 var grip = equipment.axeModel.transform.Find("LeftHandGrip");
                 if (grip) leftHandObj = grip;
                 else
                 {
                     // Deep search? Or just wait for SetupTool
                     foreach(Transform t in equipment.axeModel.transform) if (t.name == "LeftHandGrip") grip = t;
                     if(grip) leftHandObj = grip;
                 }
             }
        }

        if (leftHandObj == null) return;

        bool isHarvesting = false;
        // Check ALL layers for Harvesting state
        for (int i = 0; i < anim.layerCount; i++)
        {
            if (anim.GetCurrentAnimatorStateInfo(i).IsName("Harvesting"))
            {
                isHarvesting = true;
                break;
            }
        }

        if (isHarvesting)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
            
            // Set IK to grip
            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }
}
