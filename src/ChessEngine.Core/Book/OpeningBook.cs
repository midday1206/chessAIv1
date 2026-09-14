using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Book;

/// <summary>
/// An in-memory opening book, persisted to a JSON file. Positions are keyed by piece
/// placement + side to move + castling rights + en passant target (the move clocks are
/// ignored, since they don't change what "the same position" means). Recording a position
/// that's already known overwrites it - this book remembers the last game it was taught,
/// not a blend of many.
/// </summary>
public sealed class OpeningBook : IOpeningBook
{
    private readonly Dictionary<string, BookEntry> _entries;

    private OpeningBook(Dictionary<string, BookEntry> entries) => _entries = entries;

    public OpeningBook() : this(new Dictionary<string, BookEntry>())
    {
    }

    public int Count => _entries.Count;

    public bool TryGetMove(Board board, out Move move)
    {
        if (_entries.TryGetValue(PositionKey(board), out BookEntry entry))
        {
            move = entry.ToMove();
            return true;
        }

        move = default;
        return false;
    }

    public void Record(Board board, Move move) => _entries[PositionKey(board)] = BookEntry.FromMove(move);

    public void Save(string path)
    {
        string json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static OpeningBook Load(string path)
    {
        if (!File.Exists(path)) return new OpeningBook();

        string json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<Dictionary<string, BookEntry>>(json);
        return new OpeningBook(entries ?? new Dictionary<string, BookEntry>());
    }

    private static string PositionKey(Board board) =>
        string.Join(' ', board.ToFen().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4));

    private readonly record struct BookEntry(int FromFile, int FromRank, int ToFile, int ToRank, string Promotion, string Flags)
    {
        public static BookEntry FromMove(Move move) => new(
            move.From.File, move.From.Rank, move.To.File, move.To.Rank,
            move.Promotion.ToString(), move.Flags.ToString());

        public Move ToMove() => new(
            new Square(FromFile, FromRank),
            new Square(ToFile, ToRank),
            Enum.Parse<PieceType>(Promotion),
            Enum.Parse<MoveFlags>(Flags));
    }
}
