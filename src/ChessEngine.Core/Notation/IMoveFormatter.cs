namespace ChessEngine.Core.Notation;

/// <summary>
/// A port for turning a legal move into presentable notation. SanMoveFormatter is the first
/// implementation; other notations (or a UCI formatter) can implement this same interface
/// later without changing any caller.
/// </summary>
public interface IMoveFormatter
{
    /// <summary>Formats <paramref name="move"/> as played from <paramref name="board"/>'s current position.</summary>
    string Format(Board board, Moves.Move move);
}
