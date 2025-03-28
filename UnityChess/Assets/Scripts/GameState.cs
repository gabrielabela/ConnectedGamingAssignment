using Unity.Netcode;
using UnityChess;

/// <summary>
/// Represents the current status of the game for network synchronization.
/// </summary>
public struct SyncedGameStatus : INetworkSerializable
{
    public Side ActiveSide;
    public bool IsGameOver;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ActiveSide);
        serializer.SerializeValue(ref IsGameOver);
    }
}
