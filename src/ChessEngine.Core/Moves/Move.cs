using System;
using System.Text;

namespace ChessEngine.Core.Moves;

/// <summary>A single move from one square to another, with optional promotion and metadata flags.</summary>
public readonly struct Move : IEquatable<Move>
{
    public Square From { get; }
    public Square To { get; }
    public PieceType Promotion { get; }
    public MoveFlags Flags { get; }

    public Move(Square from, Square to, PieceType promotion = PieceType.None, MoveFlags flags = MoveFlags.None)
    {
        From = from;
        To = to;
        Promotion = promotion;
        Flags = flags;
    }

    public bool IsCapture => Flags.HasFlag(MoveFlags.Capture);
    public bool IsEnPassant => Flags.HasFlag(MoveFlags.EnPassant);
    public bool IsCastling => Flags.HasFlag(MoveFlags.Castling);
    public bool IsDoublePawnPush => Flags.HasFlag(MoveFlags.DoublePawnPush);
    public bool IsPromotion => Promotion != PieceType.None;

    public bool Equals(Move other) =>
        From == other.From && To == other.To && Promotion == other.Promotion && Flags == other.Flags;

    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(From, To, Promotion, Flags);

    /// <summary>Renders the move in UCI-style long algebraic notation, e.g. "e2e4" or "e7e8q".</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(From.ToAlgebraic()).Append(To.ToAlgebraic());

        if (IsPromotion)
        {
            sb.Append(Promotion switch
            {
                PieceType.Queen => 'q',
                PieceType.Rook => 'r',
                PieceType.Bishop => 'b',
                PieceType.Knight => 'n',
                _ => throw new InvalidOperationException($"'{Promotion}' is not a valid promotion piece.")
            });
        }

        return sb.ToString();
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);

    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}
