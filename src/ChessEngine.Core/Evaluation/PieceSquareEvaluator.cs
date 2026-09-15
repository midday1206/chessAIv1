using System;

namespace ChessEngine.Core.Evaluation;

/// <summary>
/// Scores a position by material plus piece-square placement bonuses (PieceSquareTables),
/// so the engine values central control, pawn advancement, and early king safety rather
/// than treating every legal move as equally good. Two things are tapered by GamePhase
/// instead of using one fixed value everywhere:
///  - the king's table blends from "stay safe behind castled pawns" (middlegame) to
///    "get active and centralize" (endgame) as material comes off the board;
///  - passed pawns get a bonus that grows sharply the closer they are to promoting, which
///    matters far more once there's little material left to stop them.
/// </summary>
public sealed class PieceSquareEvaluator : IPositionEvaluator
{
    // Indexed by ranks advanced from that pawn's own starting side (0 = never moved, 7 = promoted).
    private static readonly int[] PassedPawnBonusByAdvancement = { 0, 0, 10, 20, 40, 70, 120, 200 };

    public int Evaluate(Board board)
    {
        int score = 0;
        int phase = 0;
        int whiteKingFile = -1, whiteKingRank = -1;
        int blackKingFile = -1, blackKingRank = -1;

        // One pass over the board: score every non-king piece immediately and tally the game
        // phase as we go, deferring only the king (whose table depends on the final phase)
        // instead of scanning the board a second time just to compute that phase up front.
        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.IsNone) continue;

                if (piece.Type == PieceType.King)
                {
                    if (piece.Color == Color.White) (whiteKingFile, whiteKingRank) = (file, rank);
                    else (blackKingFile, blackKingRank) = (file, rank);
                    continue;
                }

                phase += GamePhase.WeightOf(piece.Type);

                int value = PieceValues.Values[piece.Type] + PieceSquareTables.GetValue(piece.Type, piece.Color, file, rank);

                if (piece.Type == PieceType.Pawn && IsPassedPawn(board, file, rank, piece.Color))
                    value += PassedPawnBonus(piece.Color, rank);

                score += piece.Color == Color.White ? value : -value;
            }
        }

        double endgameWeight = GamePhase.EndgameWeightFromPhase(phase);

        if (whiteKingFile >= 0)
            score += PieceValues.Values[PieceType.King] + PieceSquareTables.GetKingValue(Color.White, whiteKingFile, whiteKingRank, endgameWeight);
        if (blackKingFile >= 0)
            score -= PieceValues.Values[PieceType.King] + PieceSquareTables.GetKingValue(Color.Black, blackKingFile, blackKingRank, endgameWeight);

        return score;
    }

    /// <summary>
    /// A pawn is passed when no enemy pawn on its own or an adjacent file can ever block or
    /// capture it on its way to promotion.
    /// </summary>
    private static bool IsPassedPawn(Board board, int file, int rank, Color color)
    {
        int direction = color == Color.White ? 1 : -1;
        int minFile = Math.Max(0, file - 1);
        int maxFile = Math.Min(Board.BoardSize - 1, file + 1);

        for (int r = rank + direction; r >= 0 && r < Board.BoardSize; r += direction)
        {
            for (int f = minFile; f <= maxFile; f++)
            {
                Piece piece = board.GetPiece(f, r);
                if (piece.Type == PieceType.Pawn && piece.Color != color) return false;
            }
        }

        return true;
    }

    private static int PassedPawnBonus(Color color, int rank)
    {
        int advancement = color == Color.White ? rank : Board.BoardSize - 1 - rank;
        return PassedPawnBonusByAdvancement[advancement];
    }
}
