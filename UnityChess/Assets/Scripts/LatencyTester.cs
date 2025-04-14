using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// This script measures and displays network latency (ping) for a Netcode client
public class LatencyTester : NetworkBehaviour
{
    [SerializeField] private TMP_Text latencyText; // Reference to UI text element that displays the ping

    private float pingInterval = 2f; // Interval (in seconds) between ping checks
    private float lastPingTime = 0f; // Time when the last ping was sent
    private float latency = -1f; // Current measured latency in milliseconds (-1 means no data yet)

    private void Update()
    {
        // Only run ping logic if this is a client and enough time has passed since the last ping
        if (IsClient && Time.time - lastPingTime > pingInterval)
        {
            lastPingTime = Time.time; // Record current time to track ping interval
            SendPingToServerServerRpc(Time.time); // Send a ping to the server, including the current client time
        }

        // Update the latency UI text, but only on the client
        if (latencyText != null && IsClient)
        {
            // If latency has been calculated, display it; otherwise show "N/A"
            latencyText.text = latency >= 0 ? $"Ping: {latency:F1} ms" : "Ping: N/A";
        }
    }

    // Called on the server when a client sends a ping; responds with a pong
    [ServerRpc(RequireOwnership = false)] // Any client can call this, not just the object's owner
    private void SendPingToServerServerRpc(float clientSendTime, ServerRpcParams rpcParams = default)
    {
        // Respond with a ClientRpc to the specific client that sent the ping
        RespondWithPongClientRpc(clientSendTime, rpcParams.Receive.SenderClientId);
    }

    // Called on the client to receive the pong and calculate round-trip latency
    [ClientRpc]
    private void RespondWithPongClientRpc(float clientSendTime, ulong clientId)
    {
        // Ignore if this pong is not for the local client
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        // Calculate the time difference between now and when the client sent the ping
        // Multiply by 1000 to convert seconds to milliseconds
        latency = (Time.time - clientSendTime) * 1000f;
    }
}