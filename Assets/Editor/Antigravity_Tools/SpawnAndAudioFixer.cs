using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SpawnAndAudioFixer : EditorWindow
{
    [MenuItem("Tools/Fix Spawn And Audio")]
    public static void Execute()
    {
        // 1. Audio Fix
        GameObject player = GameObject.Find("Player_New");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player)
        {
            var tpcType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
            if (tpcType == null) tpcType = System.Type.GetType("ThirdPersonController, Assembly-CSharp");

            if (tpcType != null)
            {
                Component tpc = player.GetComponent(tpcType);
                if (tpc)
                {
                    SerializedObject so = new SerializedObject(tpc);
                    so.Update();

                    // Landing Sound
                    SerializedProperty landingProp = so.FindProperty("LandingAudioClip");
                    if (landingProp != null && landingProp.objectReferenceValue == null)
                    {
                        AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/StarterAssets/ThirdPersonController/Character/Sfx/Player_Land.wav");
                        if (landClip) landingProp.objectReferenceValue = landClip;
                    }

                    // Footstep Sounds
                    SerializedProperty footstepsProp = so.FindProperty("FootstepAudioClips");
                    if (footstepsProp != null && footstepsProp.arraySize == 0)
                    {
                        // Try to find grass footsteps which fit the terrain
                        string[] potentialPaths = new string[] {
                            "Assets/Characters/Player/audio/wav/footsteps/grass/Player_Footstep_Grass_Land_M_01.ogg",
                            "Assets/Characters/Player/audio/wav/footsteps/grass/Player_Footstep_Grass_Land_M_02.ogg"
                        };

                        footstepsProp.arraySize = potentialPaths.Length;
                        for(int i=0; i<potentialPaths.Length; i++)
                        {
                            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(potentialPaths[i]);
                            footstepsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                        }
                    }

                    so.ApplyModifiedProperties();
                    Debug.Log("Audio Clips Assigned.");
                }
            }
        }

        // 2. Spawn Logic
        GameObject altar = GameObject.Find("Spawn_Point");
        if (altar == null) altar = GameObject.FindGameObjectWithTag("Respawn");
        
        if (altar)
        {
            // Reset Scale
            altar.transform.localScale = new Vector3(3, 3, 3);

            // Ground Altar at 0,0
            Terrain t = Terrain.activeTerrain;
            float altarY = 0;
            if (t) altarY = t.SampleHeight(new Vector3(0, 0, 0)) + t.transform.position.y;
            
            altar.transform.position = new Vector3(0, altarY, 0);
            altar.transform.rotation = Quaternion.identity; // Face Z+
            
            EditorUtility.SetDirty(altar);

            // Move Player In Front
            if (player)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;

                // 4 meters forward (Z+)
                Vector3 playerPos = new Vector3(0, 0, 8.0f); // Altar is large (3x), go 8m out to be safe/visible
                float playerY = 0;
                if (t) playerY = t.SampleHeight(playerPos) + t.transform.position.y;
                
                player.transform.position = new Vector3(playerPos.x, playerY + 0.1f, playerPos.z);
                player.transform.rotation = Quaternion.identity; // Face Z+ (Away from Altar? Or towards? Usually forward -> Z+)

                if (cc) cc.enabled = true;
                EditorUtility.SetDirty(player);
                Debug.Log($"Player moved to {player.transform.position} (In front of Altar)");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
}
