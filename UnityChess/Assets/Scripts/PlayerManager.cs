using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;

// Manages mapping between connected clients and their assigned chess sides (White/Black)
public class PlayerManager : MonoBehaviour
{
    // Maps each client's Network ID to a Side (White or Black)
    private Dictionary<ulong, Side> playerSideMap = new();

    // Singleton instance for global access
    public static PlayerManager Instance { get; private set; }

    // Ensures singleton behavior and assigns the instance
    private void Awake()
    {
        // If an instance already exists and it’s not this one, destroy this duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // Assign this object as the singleton instance
            Instance = this;
        }
    }

    // Assigns sides (White/Black) to the first two connected clients
    public void AssignSides()
    {
        // Get a list of all currently connected clients
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        // Only assign sides if at least two players are connected
        if (connectedClients.Count >= 2)
        {
            // First client gets White
            playerSideMap[connectedClients[0].ClientId] = Side.White;
            // Second client gets Black
            playerSideMap[connectedClients[1].ClientId] = Side.Black;
        }
    }

    // Returns the Side (White/Black) for the given client ID
    public Side GetSideForClient(ulong clientId)
    {
        // Try to get the side from the dictionary; default to White if not found
        return playerSideMap.TryGetValue(clientId, out var side) ? side : Side.White;
    }

    // Returns the client ID of the player who was assigned the given side
    public ulong GetClientIdForSide(Side side)
    {
        // Iterate through the player-side map to find the client with the given side
        foreach (var pair in playerSideMap)
        {
            if (pair.Value == side)
                return pair.Key; // Return the matching client ID
        }

        // If no match is found, log an error and return 0
        Debug.LogError("[PlayerManager] No client found for side: " + side);
        return 0;
    }
}

