using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LatencyTester : NetworkBehaviour
{
    [SerializeField] private TMP_Text latencyText; // Drag your LatencyText here in Inspector

    private float pingInterval = 2f;
    private float lastPingTime = 0f;
    private float latency = -1f;

    private void Update()
    {
        if (IsClient && Time.time - lastPingTime > pingInterval)
        {
            lastPingTime = Time.time;
            SendPingToServerServerRpc(Time.time);
        }

        // Update the UI
        if (latencyText != null && IsClient)
        {
            latencyText.text = latency >= 0 ? $"Ping: {latency:F1} ms" : "Ping: N/A";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPingToServerServerRpc(float clientSendTime, ServerRpcParams rpcParams = default)
    {
        RespondWithPongClientRpc(clientSendTime, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void RespondWithPongClientRpc(float clientSendTime, ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        latency = (Time.time - clientSendTime) * 1000f;
    }
}
