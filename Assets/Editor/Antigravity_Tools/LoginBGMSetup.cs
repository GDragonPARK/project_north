using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>[Phase 8.1-3] LoginScene에 BGM_Manager를 생성하고 MenuMusic을 자동 할당한다.</summary>
public static class LoginBGMSetup
{
    private const string LOGIN_SCENE_PATH = "Assets/1.Scene/LoginScene.unity";
    private const string MENU_MUSIC_PATH  = "Assets/valheim_Data/Audio/Audio/Music/MenuMusic.ogg";

    [MenuItem("Tools/Valheim/Phase 8.1-3 Setup Login BGM")]
    public static void SetupLoginBGM()
    {
        // 1. 현재 씬 저장
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 2. LoginScene 오픈
        var loginScene = EditorSceneManager.OpenScene(LOGIN_SCENE_PATH, OpenSceneMode.Single);
        if (!loginScene.IsValid())
        {
            Debug.LogError("[LoginBGMSetup] LoginScene 열기 실패: " + LOGIN_SCENE_PATH);
            return;
        }
        Debug.Log("[LoginBGMSetup] LoginScene 열기 완료.");

        // 3. 기존 BGM_Manager 제거 (중복 방지)
        var existing = GameObject.Find("BGM_Manager");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[LoginBGMSetup] 기존 BGM_Manager 제거.");
        }

        // 4. BGM_Manager 생성 + AudioSource 부착
        var bgmManager  = new GameObject("BGM_Manager");
        var audioSource = bgmManager.AddComponent<AudioSource>();

        // 5. MenuMusic 로드 및 할당
        var menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(MENU_MUSIC_PATH);
        if (menuMusic == null)
            Debug.LogError("[LoginBGMSetup] MenuMusic 없음: " + MENU_MUSIC_PATH);

        audioSource.clip         = menuMusic;
        audioSource.playOnAwake  = true;
        audioSource.loop         = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume       = 0.4f;

        // 6. 씬 저장
        EditorUtility.SetDirty(bgmManager);
        EditorSceneManager.MarkSceneDirty(loginScene);
        EditorSceneManager.SaveScene(loginScene);

        string clipName = menuMusic != null ? menuMusic.name : "없음";
        Debug.Log("[LoginBGMSetup] ✅ 완료! clip=" + clipName + " volume=0.4 loop=true");
        EditorUtility.DisplayDialog("LoginBGM 완료", "clip: " + clipName + " / volume: 40% / loop: ON", "확인");
    }
}
