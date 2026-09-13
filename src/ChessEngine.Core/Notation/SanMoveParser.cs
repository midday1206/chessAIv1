using System;
using System.Linq;
using System.Text.RegularExpressions;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Notation;

/// <summary>
/// Parses Standard Algebraic Notation ("Nf3", "exd5", "O-O", "e8=Q") against a position's
/// legal moves. Trailing "+"/"#" check/mate markers are accepted but not required. A pawn
/// promotion letter is optional and defaults to queen, matching UciMoveParser's leniency.
/// </summary>
public sealed class SanMoveParser : IMoveParser
{
    private static readonly Regex Pattern = new(
        @"^(?<piece>[KQRBN])?(?<fromFile>[a-h])?(?<fromRank>[1-8])?(?<capture>x)?(?<to>[a-h][1-8])(=(?<promotion>[QRBN]))?[+#]?$",
        RegexOptions.Compiled);

    public bool TryParse(Board board, string input, out Move move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.Trim();

        if (TryGetCastlingSide(s, out bool isKingside))
            return TryFindCastling(board, isKingside, out move);

        Match m = Pattern.Match(s);
        if (!m.Success) return false;

        PieceType pieceType;
        if (m.Groups["piece"].Success)
        {
            if (!TryPieceType(m.Groups["piece"].Value[0], out pieceType)) return false;
        }
        else
        {
            pieceType = PieceType.Pawn;
        }

        Square to;
        try
        {
            to = Square.FromAlgebraic(m.Groups["to"].Value);
        }
        catch (FormatException)
        {
            return false;
        }

        int? fromFile = m.Groups["fromFile"].Success ? m.Groups["fromFile"].Value[0] - 'a' : null;
        int? fromRank = m.Groups["fromRank"].Success ? m.Groups["fromRank"].Value[0] - '1' : null;

        PieceType? promotion = null;
        if (m.Groups["promotion"].Success)
        {
            if (!TryPieceType(m.Groups["promotion"].Value[0], out PieceType promoType)) return false;
            promotion = promoType;
        }

        var candidates = MoveGenerator.GenerateLegalMoves(board)
            .Where(mv => mv.To == to)
            .Where(mv => board.GetPiece(mv.From).Type == pieceType)
            .Where(mv => fromFile is null || mv.From.File == fromFile)
            .Where(mv => fromRank is null || mv.From.Rank == fromRank)
            .ToList();

        Move? match = promotion.HasValue
            ? candidates.Where(mv => mv.Promotion == promotion.Value).Cast<Move?>().FirstOrDefault()
            : candidates.Where(mv => !mv.IsPromotion).Cast<Move?>().FirstOrDefault()
              ?? candidates.Where(mv => mv.Promotion == PieceType.Queen).Cast<Move?>().FirstOrDefault();

        if (match is null) return false;

        move = match.Value;
        return true;
    }

    private static bool TryPieceType(char letter, out PieceType type)
    {
        try
        {
            type = SanPieceLetters.FromLetter(letter);
            return true;
        }
        catch (FormatException)
        {
            type = PieceType.None;
            return false;
        }
    }

    private static bool TryGetCastlingSide(string input, out bool isKingside)
    {
        string normalized = input.Replace('0', 'O').TrimEnd('+', '#');

        if (normalized.Equals("O-O", StringComparison.OrdinalIgnoreCase))
        {
            isKingside = true;
            return true;
        }

        if (normalized.Equals("O-O-O", StringComparison.OrdinalIgnoreCase))
        {
            isKingside = false;
            return true;
        }

        isKingside = false;
        return false;
    }

    private static bool TryFindCastling(Board board, bool isKingside, out Move move)
    {
        move = default;
        int targetFile = isKingside ? 6 : 2;

        Move? match = MoveGenerator.GenerateLegalMoves(board)
            .Where(m => m.IsCastling && m.To.File == targetFile)
            .Cast<Move?>()
            .FirstOrDefault();

        if (match is null) return false;

        move = match.Value;
        return true;
    }
}
