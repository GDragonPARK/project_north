using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;

public class LoginUIController : MonoBehaviour
{
    public enum ConnectionState { Disconnected, Connecting, Connected, Loading, Error }

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;

    [Header("Buttons")]
    [SerializeField] private Button connectButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Log")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect logScrollRect;

    [Header("References")]
    [SerializeField] private NetworkClientManager networkManager;

    [Header("Scene Transition")]
    [SerializeField] private string mainSceneName = "main";

    private ConnectionState currentState = ConnectionState.Disconnected;

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);

        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkClientManager>();

        if (networkManager != null)
        {
            networkManager.OnStatusChanged += HandleStatusChanged;
            networkManager.OnLogMessage += AddLog;
        }

        SetState(ConnectionState.Disconnected);
        AddLog("System initialized. Ready to connect.");
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnStatusChanged -= HandleStatusChanged;
            networkManager.OnLogMessage -= AddLog;
        }
    }

    private void OnConnectClicked()
    {
        if (currentState == ConnectionState.Connecting || currentState == ConnectionState.Connected)
            return;

        string ip = ipInputField.text;
        string portStr = portInputField.text;

        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = "127.0.0.1";
            ipInputField.text = ip;
        }

        if (string.IsNullOrWhiteSpace(portStr))
        {
            portStr = "7777";
            portInputField.text = portStr;
        }

        if (!int.TryParse(portStr, out int port))
        {
            SetState(ConnectionState.Error);
            AddLog("<color=#FF4444>[ERROR]</color> Invalid port number.");
            return;
        }

        AddLog($"Attempting connection to {ip}:{port}...");
        SetState(ConnectionState.Connecting);
        networkManager.Connect(ip, port);
    }

    private void HandleStatusChanged(ConnectionState newState)
    {
        SetState(newState);

        if (newState == ConnectionState.Connected)
        {
            StartCoroutine(LoadMainSceneCoroutine());
        }
    }

    private IEnumerator LoadMainSceneCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        // Build Settings 방어 코드
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(
            System.IO.Path.Combine("Assets", mainSceneName + ".unity"));

        // 전체 Build Settings에서 씬 이름으로 검색
        bool sceneFound = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.Contains(mainSceneName))
            {
                sceneFound = true;
                break;
            }
        }

        if (!sceneFound)
        {
            AddLog($"<color=#FF4444>[ERROR]</color> Scene '{mainSceneName}' not found in Build Settings!");
            AddLog("<color=#FF4444>[ERROR]</color> Add the scene via File > Build Settings.");
            SetState(ConnectionState.Error);
            yield break;
        }

        // UI 잠금
        SetState(ConnectionState.Loading);
        ipInputField.interactable = false;
        portInputField.interactable = false;

        AddLog($"Loading scene '{mainSceneName}'...");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f) * 100f;
            statusText.text = $"Loading World... {progress:F0}%";
            yield return null;
        }

        statusText.text = "Loading World... 100%";
        AddLog("<color=#4DE94D>[SUCCESS]</color> World loaded. Entering game...");
        yield return new WaitForSeconds(0.5f);

        asyncLoad.allowSceneActivation = true;
    }

    private void SetState(ConnectionState state)
    {
        currentState = state;

        switch (state)
        {
            case ConnectionState.Disconnected:
                statusText.text = "Status: Disconnected";
                statusText.color = new Color(0.67f, 0.67f, 0.67f); // #AAAAAA
                connectButton.interactable = true;
                break;
            case ConnectionState.Connecting:
                statusText.text = "Status: Connecting...";
                statusText.color = new Color(1f, 0.84f, 0f); // #FFD700
                connectButton.interactable = false;
                break;
            case ConnectionState.Connected:
                statusText.text = "Status: Connected";
                statusText.color = new Color(0.3f, 0.9f, 0.3f); // Green
                connectButton.interactable = false;
                break;
            case ConnectionState.Loading:
                statusText.text = "Loading World...";
                statusText.color = new Color(1f, 0.84f, 0f); // #FFD700
                connectButton.interactable = false;
                break;
            case ConnectionState.Error:
                statusText.text = "Status: Connection Failed";
                statusText.color = new Color(1f, 0.27f, 0.27f); // Red
                connectButton.interactable = true;
                break;
        }
    }

    public void AddLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        logText.text += $"\n[{timestamp}] {message}";

        Canvas.ForceUpdateCanvases();
        logScrollRect.verticalNormalizedPosition = 0f;
    }
}
