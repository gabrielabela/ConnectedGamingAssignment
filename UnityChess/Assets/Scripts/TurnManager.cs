using Unity.Netcode;
using UnityEngine;
using UnityChess; // Ensure your Side enum is accessible

public class TurnManager : NetworkBehaviourSingleton<TurnManager>
{
    // NetworkVariable to hold the current turn. Ensure 'Side' is serializable.
    public NetworkVariable<Side> CurrentTurn = new NetworkVariable<Side>(Side.White);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize the turn on the server.
            CurrentTurn.Value = Side.White;
            // Subscribe to move execution events (GameManager remains untouched).
            GameManager.MoveExecutedEvent += OnMoveExecuted;
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            GameManager.MoveExecutedEvent -= OnMoveExecuted;
        }
    }

    // Called when a move has been executed.
    private void OnMoveExecuted()
    {
        // Toggle the turn only on the server.
        CurrentTurn.Value = (CurrentTurn.Value == Side.White) ? Side.Black : Side.White;
        Debug.Log($"Turn toggled. New turn: {CurrentTurn.Value}");
    }
}
