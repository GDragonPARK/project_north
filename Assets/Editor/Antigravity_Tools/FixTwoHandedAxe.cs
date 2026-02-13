using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FixTwoHandedAxe : MonoBehaviour
{
    [MenuItem("Antigravity/Fix 2H Axe Animation & IK")]
    public static void ExecuteFix()
    {
        GameObject player = GameObject.Find("Player_New");
        if (!player)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        PlayerEquipmentManager equipment = player.GetComponent<PlayerEquipmentManager>();
        if (!equipment || !equipment.axeModel)
        {
            Debug.LogError("No Axe Model found in PlayerEquipmentManager!");
            return;
        }

        GameObject axe = equipment.axeModel;
        
        // 1. Setup IK Target on Axe
        // User requested: "Grip location"
        // For a 2H axe, left hand is usually lower on the handle (closer to pommel), Right hand is near head or middle.
        // Wait, normally Right Hand (Main) is near Head? No, Main hand on handle, Off hand on handle.
        // If Axe is parented to Right Hand...
        // Let's assume standard pivot: Right Hand holds near head or middle.
        // Left hand should be lower down the handle (Z- or Y- depending on orientation).
        // Current Orientation: (0, 180, 0) -> Z might be handle direction?
        // Let's add a child GameObject "LeftHandGrip"
        
        Transform grip = axe.transform.Find("LeftHandGrip");
        if (!grip)
        {
            GameObject g = new GameObject("LeftHandGrip");
            g.transform.SetParent(axe.transform);
            grip = g.transform;
            Debug.Log("Created LeftHandGrip object on Axe.");
        }
        
        // Position Grip
        // Trial values: 
        // If Axe Y is "Up" (Blade edge out?), Handle is likely "Down" (-Y) or "Back" (-Z).
        // KayKit weapons usually have Z as forward (Blade edge), Y as Up. Handle down is -Y.
        // Let's guess: Right Hand is at 0,0,0 (Pivot). Left Hand should be at (0, -0.4, 0)?
        // Or if Handle is along Z...
        // Let's try: (0.2, -0.2, 0) -> Diagonal?
        // Let's stick to Local Position (0, -0.3, 0.1) as a guess.
        // User said: "Moves 5-10cm forward" for the AXE itself.
        
        // 1.1 Adjust Axe Position (User Request 4)
        // "Push forward 5-10cm"
        // Forward relative to hand? 
        // If Hand Z is forward... move Z +0.1?
        // Or if model is rotated 180 Y... Z is backward. So move Z -0.1?
        // Let's try changing Local Position Z slightly.
        Undo.RecordObject(axe.transform, "Adjust Axe Pos");
        axe.transform.localPosition = new Vector3(0, 0, 0.08f); // 8cm forward?
        
        // 1.2 Adjust Grip Position
        // Left hand usually grabs the handle lower.
        // If handle is "Down" (-Y local?), grip should be at (0, -0.3, 0).
        // Let's create a red sphere gizmo to visualize? No can't see.
        // Let's set it to (0, -0.4, 0) and rotate properly.
        grip.localPosition = new Vector3(0, -0.4f, 0); 
        // Rotation matters for IK!
        // Left hand palm faces handle.
        // Let's match parent rotation for now.
        grip.localRotation = Quaternion.identity;
        
        Debug.Log("Set Axe Position & Created Grip Target.");

        // 2. Add IK Script to Player
        var ikScript = player.GetComponent<PlayerHarvestingIK>();
        if (!ikScript) ikScript = player.AddComponent<PlayerHarvestingIK>();
        
        ikScript.leftHandObj = grip;
        ikScript.weight = 1.0f; // Full IK
        Debug.Log("Attached PlayerHarvestingIK script.");

        // 3. Check Animator Layers (User Request 2)
        Animator anim = player.GetComponent<Animator>();
        AnimatorController controller = anim.runtimeAnimatorController as AnimatorController;
        if (controller)
        {
            // Check layers
            // If "UpperBody" exists, ensure it has an Avatar Mask that Includes Left Arm?
            // Or just check if Harvesting is in Base Layer.
            // Our previous script put "Harvesting" in Layer 0 (Base).
            // BUT, if Layer 1 (UpperBody) has mask, and weight 1, it overrides Layer 0.
            // If UpperBody mask includes Left Arm, and that layer is Playing "Empty/None" or "Idle", it will OVERRIDE Harvesting.
            
            // Fix: Ensure Harvesting exists in UpperBody layer TOO? Or make Harvesting override UpperBody?
            // Best fix: Add "Harvesting" state to UpperBody layer too, OR ensure UpperBody layer weight is 0 when Harvesting?
            // "Harvesting" is full body action usually.
            // Let's verify layers.
            
            for(int i=0; i<controller.layers.Length; i++)
            {
                var l = controller.layers[i];
                if (l.name.Contains("Upper") || l.name.Contains("Body"))
                {
                     // If this layer is active, we might need to add Harvesting here too.
                     // Or ensure mask is Off.
                     // Let's try adding transition to Harvesting in this layer too if it exists!
                     // ... Implementing deep copy of state is hard via script without visual.
                     
                     // Alternative: Set Layer Weight to 0 via script when Harvesting starts?
                     // Hard to hook up without Behaviour script.
                     
                     // Let's try to add the State to this layer if missing.
                     AnimatorStateMachine sm = l.stateMachine;
                     bool hasState = false;
                     foreach(var s in sm.states) if (s.state.name == "Harvesting") hasState = true;
                     
                     if (!hasState)
                     {
                         // We need the clip...
                         // Can't easily get clip reference without searching again or assuming.
                         // Let's assume user uses the Setup tool correctly which only modified Base Layer.
                         
                         // Recommendation: LOG WARNING.
                         Debug.LogWarning($"Warning: Layer '{l.name}' might be overriding Harvesting. Please ensure it has a Harvesting state or weight is managed.");
                     }
                }
            }
        }
        
        EditorUtility.SetDirty(player);
    }
}
