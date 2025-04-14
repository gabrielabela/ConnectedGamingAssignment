using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

public class VisualPiece : NetworkBehaviour
{
    // Delegate for notifying when a piece is moved (drag and release)
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    public static event VisualPieceMovedAction VisualPieceMoved; // Global event for when a piece is released

    public Side PieceColor; // The side (White or Black) this piece belongs to

    // The current square this piece is on, inferred from the parent object's name
    public Square CurrentSquare => StringToSquare(transform.parent.name);

    private const float SquareCollisionRadius = 9f; // Radius to detect nearby squares
    private Camera boardCamera; // Reference to the main camera
    private Vector3 piecePositionSS; // Screen space position of the piece
    private List<GameObject> potentialLandingSquares; // Squares within range to land on
    private Transform thisTransform; // Cached transform for performance

    private void Start()
    {
        potentialLandingSquares = new List<GameObject>(); // Prepare the list used for landing detection
        thisTransform = transform; // Cache reference to this piece's transform
        boardCamera = Camera.main; // Use the main camera for drag position calculation
    }

    // Called when the piece is clicked (mouse down)
    public void OnMouseDown()
    {
        // Ignore input if the game is already over
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        // Send initial screen position to the server to start tracking drag position
        OnMouseDownServerRpc(boardCamera.WorldToScreenPoint(transform.position));

        // Store the z-depth of the piece in screen space (used for drag projection)
        piecePositionSS.z = boardCamera.WorldToScreenPoint(transform.position).z;
    }

    // Server-side RPC to register the screen-space starting position of the drag
    [ServerRpc(RequireOwnership = false)]
    private void OnMouseDownServerRpc(Vector3 pos)
    {
        piecePositionSS.x = pos.x;
        piecePositionSS.y = pos.y;
    }

    // Server-side RPC to update the piece's position while dragging
    [ServerRpc(RequireOwnership = false)]
    private void OnMouseDragServerRpc(Vector3 pos)
    {
        // Sync the dragged position across the server
        thisTransform.position = pos;
    }

    // Called every frame during a drag
    public void OnMouseDrag()
    {
        // Prevent dragging if the game is over
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        // Construct screen space position using mouse position and stored z-depth
        Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);

        // Convert that screen space position to world position
        Vector3 newWorldPos = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);

        // Move the piece visually to the new world position
        thisTransform.position = newWorldPos;

        // Inform the server of the updated piece position
        OnMouseDragServerRpc(newWorldPos);
    }

    // Called when mouse is released after dragging
    public void OnMouseUp()
    {
        // Do nothing if the game is over
        if (BoardManager.Instance != null && BoardManager.Instance.IsGameOver)
            return;

        // Notify the server that dragging has ended and pass the final position
        OnMouseUpServerRpc(thisTransform.position);
    }

    // Called on the server to handle piece release logic
    [ServerRpc(RequireOwnership = false)]
    private void OnMouseUpServerRpc(Vector3 finalPosition)
    {
        // Clear previously found squares before detecting new ones
        potentialLandingSquares.Clear();

        // Ask BoardManager to find squares near the piece within a collision radius
        BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, finalPosition, SquareCollisionRadius);

        // If no nearby squares are found, snap the piece back to its original square
        if (potentialLandingSquares.Count == 0)
        {
            thisTransform.position = transform.parent != null ? transform.parent.position : thisTransform.position;
            return;
        }

        // Find the square closest to the piece's final position
        Transform closestSquareTransform = potentialLandingSquares[0].transform;
        float shortestDistanceFromPieceSquared = (closestSquareTransform.position - finalPosition).sqrMagnitude;

        // Iterate through all candidates to find the true closest square
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

        // Trigger the piece moved event for external systems to handle (like move validation)
        VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
    }
}