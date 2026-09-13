using System.Collections.Generic;

namespace ChessEngine.Core.Evaluation;

/// <summary>Scores a position purely by the standard material point values (pawn=100 ... queen=900).</summary>
public sealed class MaterialEvaluator : IPositionEvaluator
{
    private static readonly Dictionary<PieceType, int> Values = new()
    {
        [PieceType.Pawn] = 100,
        [PieceType.Knight] = 320,
        [PieceType.Bishop] = 330,
        [PieceType.Rook] = 500,
        [PieceType.Queen] = 900,
        [PieceType.King] = 0
    };

    public int Evaluate(Board board)
    {
        int score = 0;

        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.IsNone) continue;

                int value = Values[piece.Type];
                score += piece.Color == Color.White ? value : -value;
            }
        }

        return score;
    }
}
