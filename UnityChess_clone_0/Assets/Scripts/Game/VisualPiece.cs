using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

public class VisualPiece : NetworkBehaviour
{
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    public static event VisualPieceMovedAction VisualPieceMoved;

    public Side PieceColor;

    // Store the initial square name if parent's not available.
    [SerializeField]
    private string initialSquareName;

    // Getter uses parent's name if available; otherwise, falls back to initialSquareName.
    public Square CurrentSquare
    {
        get
        {
            if (transform.parent != null && !string.IsNullOrEmpty(transform.parent.name))
            {
                Debug.Log($"[CurrentSquare] Using parent: {transform.parent.name}");
                return StringToSquare(transform.parent.name);
            }
            Debug.LogWarning($"[CurrentSquare] Using fallback: {initialSquareName}");
            return StringToSquare(initialSquareName);
        }
    }


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
        // Attempt to store initialSquareName from parent's name if available.
        if (transform.parent != null)
        {
            initialSquareName = transform.parent.name;
        }
    }

    // Public setter so BoardManager can assign the square on instantiation.
    public void SetInitialSquare(string squareName)
    {
        initialSquareName = squareName;
    }

    public void OnMouseDown()
    {
        if (enabled)
        {
            piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
        }
    }

    private void OnMouseDrag()
    {
        if (enabled)
        {
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        }
    }

    public void OnMouseUp()
    {
        if (TurnManager.Instance.CurrentTurn.Value != PieceColor)
        {
            Debug.LogWarning("Not your turn!");
            thisTransform.position = transform.parent != null ? transform.parent.position : thisTransform.position;
            return;
        }

        potentialLandingSquares.Clear();
        BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);
        if (potentialLandingSquares.Count == 0)
        {
            thisTransform.position = transform.parent != null ? transform.parent.position : thisTransform.position;
            return;
        }
        Transform closestSquareTransform = potentialLandingSquares[0].transform;
        float shortestDistanceFromPieceSquared = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;
        for (int i = 1; i < potentialLandingSquares.Count; i++)
        {
            GameObject potentialLandingSquare = potentialLandingSquares[i];
            float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;
            if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
            {
                shortestDistanceFromPieceSquared = distanceFromPieceSquared;
                closestSquareTransform = potentialLandingSquare.transform;
            }
        }
        VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
    }


}
