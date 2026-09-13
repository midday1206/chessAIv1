using System;

namespace ChessEngine.Core;

/// <summary>A board coordinate. File 0-7 maps to a-h, Rank 0-7 maps to 1-8.</summary>
public readonly struct Square : IEquatable<Square>
{
    public int File { get; }
    public int Rank { get; }

    public Square(int file, int rank)
    {
        if (file is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(file), file, "File must be between 0 and 7.");
        if (rank is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be between 0 and 7.");

        File = file;
        Rank = rank;
    }

    public string ToAlgebraic() => $"{(char)('a' + File)}{Rank + 1}";

    public static Square FromAlgebraic(string algebraic)
    {
        if (algebraic is not { Length: 2 })
            throw new FormatException($"'{algebraic}' is not a valid algebraic square.");

        int file = char.ToLowerInvariant(algebraic[0]) - 'a';
        int rank = algebraic[1] - '1';

        if (file is < 0 or > 7 || rank is < 0 or > 7)
            throw new FormatException($"'{algebraic}' is not a valid algebraic square.");

        return new Square(file, rank);
    }

    public bool Equals(Square other) => File == other.File && Rank == other.Rank;

    public override bool Equals(object? obj) => obj is Square other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(File, Rank);

    public override string ToString() => ToAlgebraic();

    public static bool operator ==(Square left, Square right) => left.Equals(right);

    public static bool operator !=(Square left, Square right) => !left.Equals(right);
}
