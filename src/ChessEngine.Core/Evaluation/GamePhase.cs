using System;

namespace ChessEngine.Core.Evaluation;

/// <summary>
/// Measures how much non-pawn material remains, from 0 (a bare-king-ish endgame) to
/// TotalPhase (full opening material). Used to taper the evaluation (king safety matters in
/// the middlegame; king activity matters in the endgame) and to decide how much deeper a
/// search can - and should - go once the position has simplified.
/// </summary>
public static class GamePhase
{
    private const int KnightPhase = 1;
    private const int BishopPhase = 1;
    private const int RookPhase = 2;
    private const int QueenPhase = 4;

    public const int TotalPhase = (4 * KnightPhase) + (4 * BishopPhase) + (4 * RookPhase) + (2 * QueenPhase);

    /// <summary>Remaining non-pawn material, clamped to [0, TotalPhase].</summary>
    public static int Compute(Board board)
    {
        int phase = 0;

        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                phase += board.GetPiece(file, rank).Type switch
                {
                    PieceType.Knight => KnightPhase,
                    PieceType.Bishop => BishopPhase,
                    PieceType.Rook => RookPhase,
                    PieceType.Queen => QueenPhase,
                    _ => 0
                };
            }
        }

        return Math.Min(phase, TotalPhase);
    }

    /// <summary>0.0 with full material (opening) up to 1.0 with (almost) no pieces left (bare endgame).</summary>
    public static double EndgameWeight(Board board) => 1.0 - ((double)Compute(board) / TotalPhase);
}
