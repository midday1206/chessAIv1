using System;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Evaluation;

/// <summary>
/// Scores a position by material plus piece-square placement bonuses (PieceSquareTables),
/// so the engine values central control, pawn advancement, and early king safety rather
/// than treating every legal move as equally good. Several things are tapered by GamePhase
/// instead of using one fixed value everywhere:
///  - the king's table blends from "stay safe behind castled pawns" (middlegame) to
///    "get active and centralize" (endgame) as material comes off the board;
///  - passed pawns get a bonus that grows sharply the closer they are to promoting, which
///    matters far more once there's little material left to stop them;
///  - a king's own pawn shield (or lack of one) is only worth penalizing while there's
///    enough material left on the board to actually exploit an open king;
///  - pushing pawns on the wing away from your own king (a pawn storm) is only worth
///    rewarding for the same reason.
/// </summary>
public sealed class PieceSquareEvaluator : IPositionEvaluator
{
    // Indexed by ranks advanced from that pawn's own starting side (0 = never moved, 7 = promoted).
    private static readonly int[] PassedPawnBonusByAdvancement = { 0, 0, 10, 20, 40, 70, 120, 200 };
    private static readonly int[] PawnStormBonusByAdvancement = { 0, 0, 5, 12, 22, 35, 50, 0 };

    private const int MissingShieldPawnPenalty = 15;
    private const int OpenFileNearKingPenalty = 30;

    public int Evaluate(Board board)
    {
        (int file, int rank)? whiteKing = null;
        (int file, int rank)? blackKing = null;

        // Pawn-storm scoring needs to know each side's own king file while it's still walking
        // the board, so find both kings first rather than deferring them to the end like the
        // (phase-dependent) king table value still is below.
        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.Type != PieceType.King) continue;

                if (piece.Color == Color.White) whiteKing = (file, rank);
                else blackKing = (file, rank);
            }
        }

        int score = 0;
        int phase = 0;

        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.IsNone || piece.Type == PieceType.King) continue;

                phase += GamePhase.WeightOf(piece.Type);

                int value = PieceValues.Values[piece.Type] + PieceSquareTables.GetValue(piece.Type, piece.Color, file, rank);

                if (piece.Type == PieceType.Pawn)
                {
                    if (IsPassedPawn(board, file, rank, piece.Color))
                        value += PassedPawnBonus(piece.Color, rank);

                    (int file, int rank)? ownKing = piece.Color == Color.White ? whiteKing : blackKing;
                    if (ownKing is { } king && IsOnAttackingWing(file, king.file))
                        value += PawnStormBonus(piece.Color, rank);
                }

                score += piece.Color == Color.White ? value : -value;
            }
        }

        double endgameWeight = GamePhase.EndgameWeightFromPhase(phase);
        double middlegameWeight = 1 - endgameWeight;

        if (whiteKing is { } wk)
        {
            score += PieceValues.Values[PieceType.King] + PieceSquareTables.GetKingValue(Color.White, wk.file, wk.rank, endgameWeight);
            score -= (int)Math.Round(KingSafetyPenalty(board, Color.White, wk.file, wk.rank) * middlegameWeight);
        }

        if (blackKing is { } bk)
        {
            score -= PieceValues.Values[PieceType.King] + PieceSquareTables.GetKingValue(Color.Black, bk.file, bk.rank, endgameWeight);
            score += (int)Math.Round(KingSafetyPenalty(board, Color.Black, bk.file, bk.rank) * middlegameWeight);
        }

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

    /// <summary>True when the file is on the opposite side of the board from the king's own file.</summary>
    private static bool IsOnAttackingWing(int pawnFile, int kingFile)
    {
        bool kingIsQueenside = kingFile <= 3;
        return kingIsQueenside ? pawnFile >= 4 : pawnFile <= 3;
    }

    private static int PawnStormBonus(Color color, int rank)
    {
        int advancement = color == Color.White ? rank : Board.BoardSize - 1 - rank;
        return PawnStormBonusByAdvancement[advancement];
    }

    /// <summary>
    /// Penalizes a thin or missing pawn shield on the king's own file and its two neighbors -
    /// but only in proportion to whether the opponent can currently actually aim pieces at
    /// it. An open file the opponent has no pieces pointed at yet is just a latent weakness
    /// (worth a small, structural nudge); the same file with enemy pieces already attacking
    /// squares next to the king is a live danger and gets close to the full penalty.
    /// </summary>
    private static int KingSafetyPenalty(Board board, Color color, int kingFile, int kingRank)
    {
        int openFiles = 0;
        int semiOpenFiles = 0;

        for (int f = Math.Max(0, kingFile - 1); f <= Math.Min(Board.BoardSize - 1, kingFile + 1); f++)
        {
            bool hasOwnPawnOnFile = false;
            bool hasAnyPawnOnFile = false;

            for (int r = 0; r < Board.BoardSize; r++)
            {
                Piece piece = board.GetPiece(f, r);
                if (piece.Type != PieceType.Pawn) continue;

                hasAnyPawnOnFile = true;
                // Deliberately lenient about *where* on the file the pawn sits: a fianchetto
                // pawn (e.g. g6, whether the king is on g1 or has walked up to g7) still
                // covers that file just fine, even though it isn't one or two ranks ahead of
                // the king the way a textbook shield pawn would be.
                if (piece.Color == color) hasOwnPawnOnFile = true;
            }

            if (!hasOwnPawnOnFile)
            {
                if (hasAnyPawnOnFile) semiOpenFiles++;
                else openFiles++;
            }
        }

        int structuralWeakness = (openFiles * OpenFileNearKingPenalty) + (semiOpenFiles * MissingShieldPawnPenalty);
        if (structuralWeakness == 0) return 0;

        double attackPressure = KingZoneAttackPressure(board, color, kingFile, kingRank);

        // An unexploited weakness still counts for something (it's an invitation), but the
        // bulk of the penalty only applies once the opponent's pieces are actually bearing
        // down on the king's own squares.
        double scale = LatentWeaknessFloor + ((1 - LatentWeaknessFloor) * attackPressure);
        return (int)Math.Round(structuralWeakness * scale);
    }

    private const double LatentWeaknessFloor = 0.25;

    /// <summary>Fraction of the king's own 3x3 zone that the opponent currently attacks.</summary>
    private static double KingZoneAttackPressure(Board board, Color color, int kingFile, int kingRank)
    {
        Color attacker = color == Color.White ? Color.Black : Color.White;
        int minFile = Math.Max(0, kingFile - 1);
        int maxFile = Math.Min(Board.BoardSize - 1, kingFile + 1);
        int minRank = Math.Max(0, kingRank - 1);
        int maxRank = Math.Min(Board.BoardSize - 1, kingRank + 1);

        int total = 0;
        int attacked = 0;

        for (int f = minFile; f <= maxFile; f++)
        {
            for (int r = minRank; r <= maxRank; r++)
            {
                total++;
                if (MoveGenerator.IsSquareAttacked(board, new Square(f, r), attacker))
                    attacked++;
            }
        }

        return total == 0 ? 0 : (double)attacked / total;
    }
}
