using System.IO;
using ChessEngine.Core;
using ChessEngine.Core.Book;
using ChessEngine.Core.Moves;

namespace ChessEngine.Tests;

public class OpeningBookTests
{
    [Test]
    public void TryGetMove_OnUnknownPosition_ReturnsFalse()
    {
        var book = new OpeningBook();
        Board board = Board.CreateStartingPosition();

        Assert.That(book.TryGetMove(board, out _), Is.False);
    }

    [Test]
    public void Record_ThenTryGetMove_ReturnsTheSameMove()
    {
        var book = new OpeningBook();
        Board board = Board.CreateStartingPosition();
        var move = new Move(Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"), flags: MoveFlags.DoublePawnPush);

        book.Record(board, move);

        Assert.That(book.TryGetMove(board, out Move retrieved), Is.True);
        Assert.That(retrieved, Is.EqualTo(move));
    }

    [Test]
    public void Record_OnTheSamePositionTwice_OverwritesTheEarlierMove()
    {
        var book = new OpeningBook();
        Board board = Board.CreateStartingPosition();

        book.Record(board, new Move(Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"), flags: MoveFlags.DoublePawnPush));
        book.Record(board, new Move(Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"), flags: MoveFlags.DoublePawnPush));

        book.TryGetMove(board, out Move retrieved);
        Assert.That(retrieved.To, Is.EqualTo(Square.FromAlgebraic("e4")));
    }

    [Test]
    public void PositionKey_IgnoresMoveClocks_SoTheSamePositionMatchesAcrossGames()
    {
        var book = new OpeningBook();
        Board firstGame = Board.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        Board laterInAnotherGame = Board.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 3 12");

        book.Record(firstGame, new Move(Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"), flags: MoveFlags.DoublePawnPush));

        Assert.That(book.TryGetMove(laterInAnotherGame, out Move retrieved), Is.True);
        Assert.That(retrieved.To, Is.EqualTo(Square.FromAlgebraic("d4")));
    }

    [Test]
    public void SaveAndLoad_RoundTripsCapturePromotionAndCastlingMoves()
    {
        string path = Path.Combine(Path.GetTempPath(), $"opening-book-test-{Path.GetRandomFileName()}.json");
        try
        {
            var book = new OpeningBook();
            Board start = Board.CreateStartingPosition();
            Board castlingBoard = Board.FromFen("8/8/8/8/8/8/8/4K2R w K - 0 1");
            Board promotionBoard = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

            book.Record(start, new Move(Square.FromAlgebraic("e2"), Square.FromAlgebraic("e4"), flags: MoveFlags.DoublePawnPush));
            book.Record(castlingBoard, new Move(Square.FromAlgebraic("e1"), Square.FromAlgebraic("g1"), flags: MoveFlags.Castling));
            book.Record(promotionBoard, new Move(Square.FromAlgebraic("e7"), Square.FromAlgebraic("e8"), PieceType.Queen));

            book.Save(path);
            OpeningBook loaded = OpeningBook.Load(path);

            Assert.That(loaded.TryGetMove(start, out Move m1), Is.True);
            Assert.That(m1.IsDoublePawnPush, Is.True);

            Assert.That(loaded.TryGetMove(castlingBoard, out Move m2), Is.True);
            Assert.That(m2.IsCastling, Is.True);
            Assert.That(m2.To, Is.EqualTo(Square.FromAlgebraic("g1")));

            Assert.That(loaded.TryGetMove(promotionBoard, out Move m3), Is.True);
            Assert.That(m3.Promotion, Is.EqualTo(PieceType.Queen));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Load_WhenFileDoesNotExist_ReturnsAnEmptyBook()
    {
        OpeningBook book = OpeningBook.Load(Path.Combine(Path.GetTempPath(), "definitely-does-not-exist.json"));

        Assert.That(book.Count, Is.EqualTo(0));
    }

    [Test]
    public void IsLocked_IsFalseByDefault_AndTrueAfterALockedRecord()
    {
        var book = new OpeningBook();
        Board board = Board.CreateStartingPosition();
        var move = new Move(Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"), flags: MoveFlags.DoublePawnPush);

        book.Record(board, move);
        Assert.That(book.IsLocked(board), Is.False);

        book.Record(board, move, locked: true);
        Assert.That(book.IsLocked(board), Is.True);
    }

    [Test]
    public void IsLocked_OnUnknownPosition_IsFalse()
    {
        var book = new OpeningBook();

        Assert.That(book.IsLocked(Board.CreateStartingPosition()), Is.False);
    }

    [Test]
    public void SaveAndLoad_RoundTripsTheLockedFlag()
    {
        string path = Path.Combine(Path.GetTempPath(), $"opening-book-test-{Path.GetRandomFileName()}.json");
        try
        {
            var book = new OpeningBook();
            Board board = Board.CreateStartingPosition();
            book.Record(board, new Move(Square.FromAlgebraic("d2"), Square.FromAlgebraic("d4"), flags: MoveFlags.DoublePawnPush), locked: true);
            book.Save(path);

            OpeningBook loaded = OpeningBook.Load(path);

            Assert.That(loaded.IsLocked(board), Is.True);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
