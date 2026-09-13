namespace ChessEngine.Core.Evaluation;

/// <summary>
/// Scores a position by material plus piece-square placement bonuses (PieceSquareTables),
/// so the engine values central control, pawn advancement, and early king safety rather
/// than treating every legal move as equally good.
/// </summary>
public sealed class PieceSquareEvaluator : IPositionEvaluator
{
    public int Evaluate(Board board)
    {
        int score = 0;

        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.IsNone) continue;

                int value = PieceValues.Values[piece.Type]
                    + PieceSquareTables.GetValue(piece.Type, piece.Color, file, rank);

                score += piece.Color == Color.White ? value : -value;
            }
        }

        return score;
    }
}
