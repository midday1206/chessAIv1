using System;
using System.Linq;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Notation;

/// <summary>
/// Parses UCI-style long algebraic move strings ("e2e4", "e7e8q") against a position's
/// legal moves. A promotion letter is optional on a promoting move - it defaults to queen,
/// matching how most UCI front-ends behave when the user omits it.
/// </summary>
public sealed class UciMoveParser : IMoveParser
{
    public bool TryParse(Board board, string input, out Move move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.Trim().ToLowerInvariant();
        if (s.Length is not (4 or 5)) return false;

        Square from, to;
        try
        {
            from = Square.FromAlgebraic(s[..2]);
            to = Square.FromAlgebraic(s[2..4]);
        }
        catch (FormatException)
        {
            return false;
        }

        PieceType? requestedPromotion = null;
        if (s.Length == 5)
        {
            requestedPromotion = s[4] switch
            {
                'q' => PieceType.Queen,
                'r' => PieceType.Rook,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                _ => (PieceType?)null
            };
            if (requestedPromotion is null) return false;
        }

        var candidates = MoveGenerator.GenerateLegalMoves(board)
            .Where(m => m.From == from && m.To == to)
            .ToList();

        Move? match = requestedPromotion.HasValue
            ? candidates.Where(m => m.Promotion == requestedPromotion.Value).Cast<Move?>().FirstOrDefault()
            : candidates.Where(m => !m.IsPromotion).Cast<Move?>().FirstOrDefault()
              ?? candidates.Where(m => m.Promotion == PieceType.Queen).Cast<Move?>().FirstOrDefault();

        if (match is null) return false;

        move = match.Value;
        return true;
    }
}
