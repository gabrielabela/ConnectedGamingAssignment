using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

public class NetworkSessionManager : MonoBehaviour
{
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private InputField sessionCodeInputField;
    [SerializeField] private TMP_Text hostCodeDisplay;
    private string currentSessionCode = "";
    private string lastSessionCode = "";

    private void Start()
    {
        RegisterCallbacks();
        UpdateConnectionStatus();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LeaveSession();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            string savedCode = PlayerPrefs.GetString("LastSessionCode", "");
            RejoinLastSession(savedCode);
        }
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
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        string sessionCode = SessionRegistry.GenerateSessionCode();
        currentSessionCode = sessionCode;
        lastSessionCode = sessionCode;

        PlayerPrefs.SetString("LastSessionCode", sessionCode);

        if (!SessionRegistry.IsValidSession(sessionCode))
        {
            Debug.Log("Adding session code to registry: " + sessionCode);
            SessionRegistry.ActiveSessions.Add(sessionCode);
        }

        Debug.Log($"Host started with code: {sessionCode}");

        NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
        {
            string clientCode = System.Text.Encoding.ASCII.GetString(request.Payload);
            bool isValid = SessionRegistry.IsValidSession(clientCode);
            Debug.Log($"Client attempting to join with code {clientCode} - Valid? {isValid}");

            response.Approved = isValid;
            response.CreatePlayerObject = false;
            response.Reason = isValid ? "" : "Invalid session code.";
        };

        NetworkManager.Singleton.StartHost();

        if (hostCodeDisplay != null)
            hostCodeDisplay.text = $"Session Code: {sessionCode}";

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Status: Host";
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        string enteredCode = sessionCodeInputField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(enteredCode))
        {
            connectionStatusText.text = "Enter a session code.";
            return;
        }

        lastSessionCode = enteredCode;
        PlayerPrefs.SetString("LastSessionCode", enteredCode);

        Debug.Log("Sending connection request with code: " + enteredCode);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(enteredCode);

        StartCoroutine(DelayedStartClient());
    }

    private System.Collections.IEnumerator DelayedStartClient()
    {
        yield return null;
        NetworkManager.Singleton.StartClient();
        connectionStatusText.text = "Connecting...";
    }

    public void LeaveSession()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("[LeaveSession] Host removing session code: " + currentSessionCode);
                SessionRegistry.ActiveSessions.Remove(currentSessionCode);
            }
            else
            {
                Debug.Log("[LeaveSession] Client leaving session. Session code remains.");
            }

            NetworkManager.Singleton.Shutdown();
        }

        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Status: Disconnected";
        }
    }

    public void RejoinLastSession(string code)
    {
        Debug.Log("Trying to rejoin session with code: " + code);
        Debug.Log("Current ActiveSessions count: " + SessionRegistry.ActiveSessions.Count);
        Debug.Log("Session exists? " + SessionRegistry.IsValidSession(code));

        if (!string.IsNullOrEmpty(code) && SessionRegistry.IsValidSession(code))
        {
            sessionCodeInputField.text = code;
            StartClient();
        }
        else
        {
            Debug.LogWarning("Session not found for code: " + code);
            connectionStatusText.text = "Session expired or not found.";
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected successfully!");
            if (connectionStatusText != null)
            {
                connectionStatusText.text = NetworkManager.Singleton.IsHost ? "Connected as Host" : "Connected as Client";
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