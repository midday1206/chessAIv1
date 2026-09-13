using System;

namespace ChessEngine.Core;

public readonly struct Piece : IEquatable<Piece>
{
    public static readonly Piece None = new(PieceType.None, Color.White);

    public PieceType Type { get; }
    public Color Color { get; }

    public Piece(PieceType type, Color color)
    {
        Type = type;
        Color = color;
    }

    public bool IsNone => Type == PieceType.None;

    /// <summary>Converts the piece to its FEN character (uppercase for White, lowercase for Black).</summary>
    public char ToFenChar()
    {
        if (IsNone) return ' ';

        char c = Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown piece type.")
        };

        return Color == Color.White ? char.ToUpperInvariant(c) : c;
    }

    /// <summary>Parses a single FEN piece character (e.g. 'K', 'p') into a Piece.</summary>
    public static Piece FromFenChar(char c)
    {
        Color color = char.IsUpper(c) ? Color.White : Color.Black;
        PieceType type = char.ToLowerInvariant(c) switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => throw new FormatException($"'{c}' is not a valid FEN piece character.")
        };

        return new Piece(type, color);
    }

    public bool Equals(Piece other) => Type == other.Type && Color == other.Color;

    public override bool Equals(object? obj) => obj is Piece other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Type, Color);

    public override string ToString() => IsNone ? "None" : $"{Color} {Type}";

    public static bool operator ==(Piece left, Piece right) => left.Equals(right);

    public static bool operator !=(Piece left, Piece right) => !left.Equals(right);
}
