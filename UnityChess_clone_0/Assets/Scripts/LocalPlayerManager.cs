using UnityChess;
using UnityEngine;

// This class manages information related to the local player in the chess game.
// It stores which side (White or Black) the local player is assigned to.
public class LocalPlayerManager : MonoBehaviour
{
    // Singleton instance to ensure only one LocalPlayerManager exists and can be accessed globally.
    public static LocalPlayerManager Instance { get; private set; }

    // The side (White or Black) assigned to this player.
    // It's set externally after player side assignment is determined.
    public Side MySide { get; set; }

    // Called when the GameObject is initialized (before Start).
    private void Awake()
    {
        // Enforce the Singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy this duplicate instance
        }
        else
        {
            Instance = this; // Assign this as the singleton instance
            DontDestroyOnLoad(gameObject); // Persist this object between scene loads
        }
    }
}
