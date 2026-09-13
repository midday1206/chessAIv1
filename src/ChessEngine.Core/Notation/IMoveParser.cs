namespace ChessEngine.Core.Notation;

/// <summary>
/// A port for turning user/protocol input into one of a position's legal moves.
/// UciMoveParser is the first implementation; a SAN parser can implement this
/// same interface later without changing any caller.
/// </summary>
public interface IMoveParser
{
    /// <summary>
    /// Attempts to resolve <paramref name="input"/> to one of <paramref name="board"/>'s legal
    /// moves. Returns false (rather than throwing) for unrecognized syntax or illegal moves.
    /// </summary>
    bool TryParse(Board board, string input, out Moves.Move move);
}
