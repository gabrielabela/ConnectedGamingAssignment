using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;
//using static UnityEditor.PlayerSettings;

public class VisualPiece : NetworkBehaviour
{
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    public static event VisualPieceMovedAction VisualPieceMoved;

    public Side PieceColor;

    // We assume the piece's current square is given by its parent’s name.
    public Square CurrentSquare => StringToSquare(transform.parent.name);

    private const float SquareCollisionRadius = 9f;
    private Camera boardCamera;
    private Vector3 piecePositionSS;
    private List<GameObject> potentialLandingSquares;
    private Transform thisTransform;

    private void Start()
    {
        potentialLandingSquares = new List<GameObject>();
        thisTransform = transform;
        boardCamera = Camera.main;
    }


    public void OnMouseDown()
    {
        // Do not allow input if the game is over.
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        OnMouseDownServerRpc(boardCamera.WorldToScreenPoint(transform.position));
        piecePositionSS.z = boardCamera.WorldToScreenPoint(transform.position).z;
    }


    [ServerRpc(RequireOwnership = false)]
    private void OnMouseDownServerRpc(Vector3 pos)
    {
        piecePositionSS.x = pos.x;
        piecePositionSS.y = pos.y;
    }


    [ServerRpc(RequireOwnership = false)]
    private void OnMouseDragServerRpc(Vector3 pos)
    {
        thisTransform.position = pos;

    }

    public void OnMouseDrag()
    {
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
        Vector3 newWorldPos = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        thisTransform.position = newWorldPos;
        OnMouseDragServerRpc(newWorldPos);
    }

    public void OnMouseUp()
    {
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        OnMouseUpServerRpc(thisTransform.position);
    }

    // This ServerRpc is called when the owner releases the piece.
    [ServerRpc(RequireOwnership = false)]
    private void OnMouseUpServerRpc(Vector3 finalPosition)
    {
        //// Validate turn on the server.
        //if (TurnManager.Instance.CurrentTurn.Value != PieceColor)
        //{
        //    Debug.LogWarning("Not your turn!");
        //    // Reset the piece's position on the server.
        //    thisTransform.position = transform.parent != null ? transform.parent.position : thisTransform.position;
        //    return;
        //}

        // Find the nearest landing square based on the passed finalPosition.
        potentialLandingSquares.Clear();
        BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, finalPosition, SquareCollisionRadius);
        if (potentialLandingSquares.Count == 0)
        {
            // If no valid square is found, reset the piece's position.
            thisTransform.position = transform.parent != null ? transform.parent.position : thisTransform.position;
            return;
        }

        Transform closestSquareTransform = potentialLandingSquares[0].transform;
        float shortestDistanceFromPieceSquared = (closestSquareTransform.position - finalPosition).sqrMagnitude;
        for (int i = 1; i < potentialLandingSquares.Count; i++)
        {
            GameObject potentialLandingSquare = potentialLandingSquares[i];
            float distanceFromPieceSquared = (potentialLandingSquare.transform.position - finalPosition).sqrMagnitude;
            if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
            {
                shortestDistanceFromPieceSquared = distanceFromPieceSquared;
                closestSquareTransform = potentialLandingSquare.transform;
            }
        }

        // Fire an event so other systems (such as move validation, promotion, etc.) can handle the move.
        VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
    }
}