namespace ChessEngine.Core;

/// <summary>The status of the side to move in a given position.</summary>
public enum GameStatus
{
    InProgress,
    Check,
    Checkmate,
    Stalemate
}
