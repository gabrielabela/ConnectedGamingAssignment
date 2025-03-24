using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkSessionManager : MonoBehaviour
{
    // Reference to a UI Text element to show connection status (optional)
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private InputField sessionCodeInputField;

    private void Start()
    {
        // Optionally update the connection status
        RegisterCallbacks();
        UpdateConnectionStatus();
    }

    /// <summary>
    /// Registers network connection callbacks for state handling.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            // Removed OnTransportFailure subscription since it's no longer available.
        }
    }

    /// <summary>
    /// Starts a host session (server + client).
    /// </summary>
    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            UpdateConnectionStatus();
            Debug.Log("Started as Host.");
        }
    }

    /// <summary>
    /// Starts a client session after validating the session code.
    /// </summary>
    public void StartClient()
    {
        // Example validation: check that a session code was provided.
        if (sessionCodeInputField != null && string.IsNullOrEmpty(sessionCodeInputField.text))
        {
            Debug.LogError("Invalid session code!");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Invalid session code!";
            }
            return;
        }

        // In a real-world scenario, you might use the session code to configure the connection.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            UpdateConnectionStatus();
            Debug.Log("Attempting to start as Client.");
        }
    }

    /// <summary>
    /// Leaves the current session by shutting down the network.
    /// </summary>
    public void LeaveSession()
    {
        if (NetworkManager.Singleton != null)
        {
            // Shutdown works for both host and client.
            NetworkManager.Singleton.Shutdown();
            UpdateConnectionStatus();
            Debug.Log("Left the session.");
        }
    }

    /// <summary>
    /// Rejoins the session by trying to start the client again.
    /// This is a simple rejoin logic; for production you may implement exponential backoff or user prompts.
    /// </summary>
    public void RejoinSession()
    {
        if (NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            StartClient();
        }
    }

    /// <summary>
    /// Callback triggered when a client connects successfully.
    /// </summary>
    /// <param name="clientId">ID of the connected client.</param>
    private void OnClientConnected(ulong clientId)
    {
        // Only update UI if the local client connected.
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected successfully!");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Connected as Client";
            }
        }
    }

    /// <summary>
    /// Callback triggered when a client disconnects.
    /// </summary>
    /// <param name="clientId">ID of the disconnected client.</param>
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client disconnected.");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = "Disconnected";
            }
            // Optionally: automatically attempt rejoining or notify the player.
        }
    }

    /// <summary>
    /// Updates the UI element to reflect the current connection state.
    /// </summary>
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
