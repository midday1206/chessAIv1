using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Search;

/// <summary>
/// A port for choosing a move to play. MinimaxSearch is the first implementation; a
/// different search strategy can implement this same interface later without changing
/// whatever calls it (the CLI today, potentially a UCI engine loop later).
/// </summary>
public interface IMoveSearcher
{
    /// <summary>Searches to the given depth (in plies) and returns the best move, or null if there is none.</summary>
    Move? FindBestMove(Board board, int depth);
}
