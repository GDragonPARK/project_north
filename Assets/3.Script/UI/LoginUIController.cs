using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using Mirror;

public class LoginUIController : MonoBehaviour
{
    public enum ConnectionState { Disconnected, Connecting, Connected, Loading, Error }

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField pwInputField;

    [Header("Buttons")]
    [SerializeField] private Button connectButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Log")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect logScrollRect;

    [Header("Scene Transition")]
    [SerializeField] private string mainSceneName = "main";

    private ConnectionState currentState = ConnectionState.Disconnected;
    private MySqlAuthenticator authenticator;

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);

        if (NetworkManager.singleton != null)
        {
            authenticator = NetworkManager.singleton.GetComponent<MySqlAuthenticator>();
            if (authenticator != null)
            {
                authenticator.OnAuthResponse += HandleAuthResponse;
            }
        }

        NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;

        SetState(ConnectionState.Disconnected);
        AddLog("System initialized. Ready to connect.");
    }

    private void OnDestroy()
    {
        if (authenticator != null)
        {
            authenticator.OnAuthResponse -= HandleAuthResponse;
        }

        NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
    }

    public void OnConnectClicked()
    {
        Debug.Log("<color=yellow>[LoginUI] Connect Button Clicked!</color>");

        if (currentState == ConnectionState.Connecting || currentState == ConnectionState.Connected)
            return;

        string ip = ipInputField.text;
        string portStr = portInputField.text;
        string id = idInputField.text;
        string pw = pwInputField.text;

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

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(pw))
        {
            SetState(ConnectionState.Error);
            AddLog("<color=#FF4444>[ERROR]</color> Please enter your ID and Password.");
            return;
        }

        if (!int.TryParse(portStr, out int port))
        {
            SetState(ConnectionState.Error);
            AddLog("<color=#FF4444>[ERROR]</color> Invalid port number.");
            return;
        }

        AddLog($"Attempting connection to {ip}:{port}...");
        SetState(ConnectionState.Connecting);

        if (NetworkManager.singleton == null)
        {
            SetState(ConnectionState.Error);
            AddLog("<color=#FF4444>[ERROR]</color> NetworkManager not found.");
            return;
        }

        if (authenticator == null)
        {
            authenticator = NetworkManager.singleton.GetComponent<MySqlAuthenticator>();
            if (authenticator != null)
            {
                // Unsubscribe first just in case to avoid double subscription
                authenticator.OnAuthResponse -= HandleAuthResponse;
                authenticator.OnAuthResponse += HandleAuthResponse;
            }
            else
            {
                SetState(ConnectionState.Error);
                AddLog("<color=#FF4444>[ERROR]</color> MySqlAuthenticator not found on NetworkManager.");
                return;
            }
        }

        authenticator.clientUsername = id;
        authenticator.clientPassword = pw;

        NetworkManager.singleton.networkAddress = ip;

        // Try to set the port if we can find a KCP transport
        var transport = NetworkManager.singleton.GetComponent<kcp2k.KcpTransport>();
        if (transport != null)
        {
            transport.Port = (ushort)port;
        }

        NetworkManager.singleton.StartClient();
    }

    private void HandleAuthResponse(bool success, string message)
    {
        if (success)
        {
            AddLog($"<color=#4DE94D>[SUCCESS]</color> {message}");
            SetState(ConnectionState.Connected);
            // Removed StartCoroutine(LoadMainSceneCoroutine()); Mirror handles scene transition automatically.
        }
        else
        {
            AddLog($"<color=#FF4444>[ERROR]</color> {message}");
            SetState(ConnectionState.Error);
            NetworkManager.singleton.StopClient();
        }
    }

    private void HandleClientDisconnected()
    {
        if (currentState == ConnectionState.Connecting)
        {
            AddLog("<color=#FF4444>[ERROR]</color> Connection failed or server is offline.");
            SetState(ConnectionState.Error);
        }
        else if (currentState == ConnectionState.Connected || currentState == ConnectionState.Loading)
        {
            AddLog("<color=#FF4444>[ERROR]</color> Disconnected from server.");
            SetState(ConnectionState.Error);
        }
    }

    // private IEnumerator LoadMainSceneCoroutine() ... Removed as Mirror handles scene transition


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
                SetButtonText("CONNECTING...");
                break;
            case ConnectionState.Connected:
                statusText.text = "Status: Connected";
                statusText.color = new Color(0.3f, 0.9f, 0.3f); // Green
                connectButton.interactable = false;
                SetButtonText("CONNECTING...");
                break;
            case ConnectionState.Loading:
                statusText.text = "Loading World...";
                statusText.color = new Color(1f, 0.84f, 0f); // #FFD700
                connectButton.interactable = false;
                SetButtonText("CONNECTING...");
                break;
            case ConnectionState.Error:
                statusText.text = "Status: Connection Failed";
                statusText.color = new Color(1f, 0.27f, 0.27f); // Red
                connectButton.interactable = true;
                SetButtonText("CONNECT");
                break;
        }
    }

    private void SetButtonText(string text)
    {
        if (connectButton != null)
        {
            TextMeshProUGUI btnText = connectButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = text;
            }
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
