using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class CleanDuplicateStats : MonoBehaviour
{
    [MenuItem("Antigravity/Phase 11.11 - Clean Duplicate Stats")]
    public static void ExecuteCleaning()
    {
        Debug.Log("<color=red><b>[Phase 11.11]</b></color> Commencing The Great Cleansing of CharacterStats...");

        GameObject player = GameObject.Find("Player_New");
        if (player == null)
        {
            Debug.LogError("Player_New not found!");
            return;
        }

        // Get all CharacterStats on Player_New
        CharacterStats[] statsArray = player.GetComponents<CharacterStats>();
        if (statsArray.Length == 0)
        {
            Debug.LogWarning("No CharacterStats found on Player_New!");
            return;
        }

        Debug.Log($"Found {statsArray.Length} CharacterStats on Player_New.");

        // Keep the first one, destroy the rest
        CharacterStats validStats = statsArray[0];
        int destroyedCount = 0;

        for (int i = 1; i < statsArray.Length; i++)
        {
            DestroyImmediate(statsArray[i]);
            destroyedCount++;
        }

        Debug.Log($"<color=orange>Destroyed {destroyedCount} duplicate CharacterStats components.</color>");

        // Explicitly assign StaminaBar_Fill to the remaining instance
        GameObject staminaGauge = GameObject.Find("StaminaBar_Fill");
        if (staminaGauge != null)
        {
            Image img = staminaGauge.GetComponent<Image>();
            if (img != null)
            {
                validStats.staminaBar = img;
                Debug.Log("Successfully linked StaminaBar_Fill to the true CharacterStats instance.");
            }
        }
        else
        {
            Debug.LogWarning("StaminaBar_Fill object not found in the scene.");
        }

        // Mark scene as dirty to save the component removals
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        // Save the scene
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=cyan><b>[The Great Cleansing Complete]</b></color> Duplicates purged and bindings restored.");
    }
}
