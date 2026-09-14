namespace ChessEngine.Core.Book;

/// <summary>
/// A port for a fixed-repertoire opening book: look up whatever move was previously recorded
/// for a position, or record one after it's actually played. This isn't a statistics-driven
/// book built from a game database - it's a way to pin a chosen repertoire by playing it once.
/// A heavier, database-trained book could implement this same interface later.
/// </summary>
public interface IOpeningBook
{
    /// <summary>Looks up a previously recorded move for this exact position, if any.</summary>
    bool TryGetMove(Board board, out Moves.Move move);

    /// <summary>Records (or overwrites) the move played from this position.</summary>
    void Record(Board board, Moves.Move move);
}
