using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NetworkSessionManager : MonoBehaviour
{
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private InputField sessionCodeInputField;

    private void Start()
    {
        RegisterCallbacks();
        UpdateConnectionStatus();
    }

    private void RegisterCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            UpdateConnectionStatus();
            Debug.Log("Started as Host.");
        }
    }

    public void StartClient()
    {
        if (sessionCodeInputField != null && string.IsNullOrEmpty(sessionCodeInputField.text))
        {
            Debug.LogError("Invalid session code!");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Invalid session code!";
            }
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            UpdateConnectionStatus();
            Debug.Log("Attempting to start as Client.");
        }
    }

    public void LeaveSession()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            UpdateConnectionStatus();
            Debug.Log("Left the session.");
        }
    }

    public void RejoinSession()
    {
        if (NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            StartClient();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected successfully!");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Connected as Client";
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client disconnected.");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Disconnected";
            }
        }
    }

    private void UpdateConnectionStatus()
    {
        if (NetworkManager.Singleton == null || connectionStatusText == null)
            return;

        if (NetworkManager.Singleton.IsHost)
        {
            connectionStatusText.text = "Status: Host";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            connectionStatusText.text = "Status: Client";
        }
        else
        {
            connectionStatusText.text = "Status: Disconnected";
        }
    }
}
