using System;
using System.Collections.Generic;

namespace ChessEngine.Core.Evaluation;

/// <summary>
/// Tomasz Michniewski's widely-used "simplified evaluation function" piece-square tables
/// (in centipawns), one flat 64-entry array per piece type, written a8..h8 down to a1..h1
/// as White would see the board. Values reward central control, pawn advancement, knight/
/// bishop activity, rook placement, and early king safety.
/// </summary>
internal static class PieceSquareTables
{
    private static readonly int[] Pawn =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    private static readonly int[] Knight =
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    };

    private static readonly int[] Bishop =
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    private static readonly int[] Rook =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
         5, 10, 10, 10, 10, 10, 10,  5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
         0,  0,  0,  5,  5,  0,  0,  0
    };

    private static readonly int[] Queen =
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    // King safety table for the middlegame: favors staying tucked behind castled pawns.
    private static readonly int[] KingMiddlegame =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    };

    // Endgame king table: the opposite instinct applies once material thins out - an
    // uncastled/exposed king stops mattering and an active, centralized king (to shepherd
    // its own pawns or blockade the opponent's) becomes a real asset.
    private static readonly int[] KingEndgame =
    {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    };

    private static readonly Dictionary<PieceType, int[]> Tables = new()
    {
        [PieceType.Pawn] = Pawn,
        [PieceType.Knight] = Knight,
        [PieceType.Bishop] = Bishop,
        [PieceType.Rook] = Rook,
        [PieceType.Queen] = Queen
    };

    /// <summary>The bonus/penalty for a non-king piece of the given type and color sitting on (file, rank).</summary>
    public static int GetValue(PieceType type, Color color, int file, int rank)
    {
        if (!Tables.TryGetValue(type, out int[]? table))
            throw new ArgumentOutOfRangeException(nameof(type), type, "No piece-square table for this piece type.");

        return table[RowMajorIndex(color, file, rank)];
    }

    /// <summary>
    /// The king's bonus/penalty, blended between the middlegame and endgame tables by
    /// <paramref name="endgameWeight"/> (0 = pure middlegame table, 1 = pure endgame table).
    /// </summary>
    public static int GetKingValue(Color color, int file, int rank, double endgameWeight)
    {
        int index = RowMajorIndex(color, file, rank);
        double blended = (KingMiddlegame[index] * (1 - endgameWeight)) + (KingEndgame[index] * endgameWeight);
        return (int)Math.Round(blended);
    }

    // Tables are written rank8 (row 0) down to rank1 (row 7) from White's point of view;
    // mirror vertically for Black so e.g. a pawn on its 2nd rank always maps to the same row.
    private static int RowMajorIndex(Color color, int file, int rank)
    {
        int row = color == Color.White ? 7 - rank : rank;
        return (row * 8) + file;
    }
}
