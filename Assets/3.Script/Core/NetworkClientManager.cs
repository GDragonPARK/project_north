using UnityEngine;
using System;
using System.Collections;

public class NetworkClientManager : MonoBehaviour
{
    public event Action<LoginUIController.ConnectionState> OnStatusChanged;
    public event Action<string> OnLogMessage;

    [Header("Dummy Settings")]
    [SerializeField] private float connectionDelay = 2f;
    [SerializeField] private bool simulateSuccess = true;

    public void Connect(string ip, int port)
    {
        StartCoroutine(SimulateConnection(ip, port));
    }

    private IEnumerator SimulateConnection(string ip, int port)
    {
        OnLogMessage?.Invoke($"Resolving host {ip}...");
        yield return new WaitForSeconds(connectionDelay * 0.3f);

        OnLogMessage?.Invoke($"Establishing TCP connection on port {port}...");
        yield return new WaitForSeconds(connectionDelay * 0.4f);

        OnLogMessage?.Invoke("Performing handshake...");
        yield return new WaitForSeconds(connectionDelay * 0.3f);

        if (simulateSuccess)
        {
            OnLogMessage?.Invoke("<color=#4DE94D>[SUCCESS]</color> Connected to server.");
            OnStatusChanged?.Invoke(LoginUIController.ConnectionState.Connected);
        }
        else
        {
            OnLogMessage?.Invoke("<color=#FF4444>[FAILED]</color> Connection timed out.");
            OnStatusChanged?.Invoke(LoginUIController.ConnectionState.Error);
        }
    }

    public void Disconnect()
    {
        StopAllCoroutines();
        OnLogMessage?.Invoke("Disconnected from server.");
        OnStatusChanged?.Invoke(LoginUIController.ConnectionState.Disconnected);
    }
}
