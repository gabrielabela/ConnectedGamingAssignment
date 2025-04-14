using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

// Handles session-based multiplayer connections using Unity Netcode and session codes
public class NetworkSessionManager : MonoBehaviour
{
    [SerializeField] private TMP_Text connectionStatusText; // UI text that displays connection status
    [SerializeField] private TMP_InputField sessionCodeInputField; // Input field where clients enter session code
    [SerializeField] private TMP_Text hostCodeDisplay; // UI text showing the session code when hosting

    private string currentSessionCode = ""; // Session code assigned when hosting
    private string lastSessionCode = ""; // Used to rejoin a previous session (saved in PlayerPrefs)

    private void Start()
    {
        RegisterCallbacks(); // Subscribe to Netcode connection events
        UpdateConnectionStatus(); // Update the UI to show initial status
    }

    private void Update()
    {
        // Pressing "2" leaves the session
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LeaveSession();
        }

        // Pressing "3" tries to rejoin the last session using stored session code
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            string savedCode = PlayerPrefs.GetString("LastSessionCode", "");
            RejoinLastSession(savedCode);
        }
    }

    // Registers connection-related callbacks from Unity Netcode
    private void RegisterCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    // Called when player starts hosting a session
    public void StartHost()
    {
        if (NetworkManager.Singleton == null) return;

        // Enable connection approval so we can filter valid clients
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        // Generate a random session code and store it
        string sessionCode = SessionRegistry.GenerateSessionCode();
        currentSessionCode = sessionCode;
        lastSessionCode = sessionCode;

        // Save session code for rejoin feature
        PlayerPrefs.SetString("LastSessionCode", sessionCode);

        // If the code doesn't already exist in the registry, add it
        if (!SessionRegistry.IsValidSession(sessionCode))
        {
            Debug.Log("Adding session code to registry: " + sessionCode);
            SessionRegistry.ActiveSessions.Add(sessionCode);
        }

        Debug.Log($"Host started with code: {sessionCode}");

        // Define how new clients are approved when they try to join
        NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
        {
            string clientCode = System.Text.Encoding.ASCII.GetString(request.Payload); // Decode client session code
            bool isValid = SessionRegistry.IsValidSession(clientCode); // Check if it exists in registry
            Debug.Log($"Client attempting to join with code {clientCode} - Valid? {isValid}");

            response.Approved = isValid; // Accept or reject based on validity
            response.CreatePlayerObject = false; // Don’t auto-spawn player object
            response.Reason = isValid ? "" : "Invalid session code.";
        };

        // Start hosting the session
        NetworkManager.Singleton.StartHost();

        // Show session code in UI
        if (hostCodeDisplay != null)
            hostCodeDisplay.text = $"Session Code: {sessionCode}";

        // Update connection status text
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Status: Host";
        }
    }

    // Called when the user attempts to join a session as a client
    public void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true; // Enable connection approval on client side too

        // Get code from user input, sanitize it
        string enteredCode = sessionCodeInputField.text.Trim().ToUpper();

        // If no code entered, show warning
        if (string.IsNullOrEmpty(enteredCode))
        {
            connectionStatusText.text = "Enter a session code.";
            return;
        }

        // Store session code for rejoin support
        lastSessionCode = enteredCode;
        PlayerPrefs.SetString("LastSessionCode", enteredCode);

        Debug.Log("Sending connection request with code: " + enteredCode);

        // Pass session code to server via connection payload
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(enteredCode);

        // Delay client start by 1 frame to allow config setup
        StartCoroutine(DelayedStartClient());
    }

    // Waits 1 frame then connects client and updates UI
    private System.Collections.IEnumerator DelayedStartClient()
    {
        yield return null;
        NetworkManager.Singleton.StartClient();
        connectionStatusText.text = "Connecting...";
    }

    // Called when a player manually or programmatically leaves a session
    public void LeaveSession()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Only host should remove the session code from the registry
                Debug.Log("[LeaveSession] Host removing session code: " + currentSessionCode);
                SessionRegistry.ActiveSessions.Remove(currentSessionCode);
            }
            else
            {
                Debug.Log("[LeaveSession] Client leaving session. Session code remains.");
            }

            // Shut down the Netcode connection
            NetworkManager.Singleton.Shutdown();
        }

        // Update UI to reflect disconnection
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Status: Disconnected";
        }
    }

    // Attempts to rejoin a session using a previously saved session code
    public void RejoinLastSession(string code)
    {
        Debug.Log("Trying to rejoin session with code: " + code);
        Debug.Log("Current ActiveSessions count: " + SessionRegistry.ActiveSessions.Count);
        Debug.Log("Session exists? " + SessionRegistry.IsValidSession(code));

        // Check if the session is still valid
        if (!string.IsNullOrEmpty(code) && SessionRegistry.IsValidSession(code))
        {
            sessionCodeInputField.text = code; // Auto-fill input field
            StartClient(); // Try to reconnect as client
        }
        else
        {
            // Session no longer valid or never existed
            Debug.LogWarning("Session not found for code: " + code);
            connectionStatusText.text = "Session expired or not found.";
        }
    }

    // Called when a client connects successfully
    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected successfully!");

            if (connectionStatusText != null)
            {
                connectionStatusText.text = NetworkManager.Singleton.IsHost
                    ? "Connected as Host"
                    : "Connected as Client";
            }
        }
    }

    // Called when the local client is disconnected from the session
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

    // Updates the connection status UI on start or after reconnects
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