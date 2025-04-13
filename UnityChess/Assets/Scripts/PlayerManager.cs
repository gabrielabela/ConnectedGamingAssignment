using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private Dictionary<ulong, Side> playerSideMap = new();

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Assign players to sides (first is White, second is Black)
    public void AssignSides()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        if (connectedClients.Count >= 2)
        {
            playerSideMap[connectedClients[0].ClientId] = Side.White;
            playerSideMap[connectedClients[1].ClientId] = Side.Black;
        }
    }

    // Allow retrieval
    public Side GetSideForClient(ulong clientId)
    {
        return playerSideMap.TryGetValue(clientId, out var side) ? side : Side.White;
    }
    public ulong GetClientIdForSide(Side side)
    {
        foreach (var pair in playerSideMap)
        {
            if (pair.Value == side)
                return pair.Key;
        }

        Debug.LogError("[PlayerManager] No client found for side: " + side);
        return 0; // fallback to host
    }

}
