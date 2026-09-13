using System;

namespace ChessEngine.Core.Notation;

/// <summary>Shared piece-letter mapping used by both the SAN formatter and the SAN parser.</summary>
internal static class SanPieceLetters
{
    public static char ToLetter(PieceType type) => type switch
    {
        PieceType.Knight => 'N',
        PieceType.Bishop => 'B',
        PieceType.Rook => 'R',
        PieceType.Queen => 'Q',
        PieceType.King => 'K',
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Pawns and None have no SAN piece letter.")
    };

    public static PieceType FromLetter(char letter) => char.ToUpperInvariant(letter) switch
    {
        'N' => PieceType.Knight,
        'B' => PieceType.Bishop,
        'R' => PieceType.Rook,
        'Q' => PieceType.Queen,
        'K' => PieceType.King,
        _ => throw new FormatException($"'{letter}' is not a valid SAN piece letter.")
    };
}
