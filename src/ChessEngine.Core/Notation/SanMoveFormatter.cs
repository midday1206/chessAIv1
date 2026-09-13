using System.Linq;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Notation;

/// <summary>Formats legal moves as Standard Algebraic Notation, e.g. "Nf3", "exd5", "O-O", "e8=Q#".</summary>
public sealed class SanMoveFormatter : IMoveFormatter
{
    public string Format(Board board, Move move)
    {
        Piece piece = board.GetPiece(move.From);
        string san;

        if (move.IsCastling)
        {
            san = move.To.File > move.From.File ? "O-O" : "O-O-O";
        }
        else if (piece.Type == PieceType.Pawn)
        {
            string source = move.IsCapture ? $"{FileLetter(move.From.File)}x" : "";
            san = source + move.To.ToAlgebraic();
            if (move.IsPromotion) san += $"={SanPieceLetters.ToLetter(move.Promotion)}";
        }
        else
        {
            string disambiguation = Disambiguate(board, move, piece);
            string captureMark = move.IsCapture ? "x" : "";
            san = $"{SanPieceLetters.ToLetter(piece.Type)}{disambiguation}{captureMark}{move.To.ToAlgebraic()}";
        }

        return san + CheckSuffix(board, move);
    }

    private static string Disambiguate(Board board, Move move, Piece piece)
    {
        var others = MoveGenerator.GenerateLegalMoves(board)
            .Where(m => m.To == move.To && m.From != move.From && board.GetPiece(m.From).Type == piece.Type)
            .ToList();

        if (others.Count == 0) return "";

        if (others.All(m => m.From.File != move.From.File)) return FileLetter(move.From.File);
        if (others.All(m => m.From.Rank != move.From.Rank)) return (move.From.Rank + 1).ToString();

        return move.From.ToAlgebraic();
    }

    private static string CheckSuffix(Board board, Move move)
    {
        Board resulting = board.Clone();
        resulting.ApplyMove(move);

        return MoveGenerator.GetGameStatus(resulting) switch
        {
            GameStatus.Checkmate => "#",
            GameStatus.Check => "+",
            _ => ""
        };
    }

    private static string FileLetter(int file) => ((char)('a' + file)).ToString();
}
