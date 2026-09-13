using ChessEngine.Core;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;

namespace ChessEngine.Tests;

public class UciMoveParserTests
{
    private readonly UciMoveParser _parser = new();

    [Test]
    public void TryParse_OrdinaryMove_Succeeds()
    {
        Board board = Board.CreateStartingPosition();

        bool ok = _parser.TryParse(board, "e2e4", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("e2")));
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("e4")));
        Assert.That(move.IsDoublePawnPush, Is.True);
    }

    [Test]
    public void TryParse_IsCaseInsensitiveAndTrimsWhitespace()
    {
        Board board = Board.CreateStartingPosition();

        bool ok = _parser.TryParse(board, "  E2E4  ", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("e4")));
    }

    [Test]
    public void TryParse_IllegalMove_Fails()
    {
        Board board = Board.CreateStartingPosition();

        bool ok = _parser.TryParse(board, "e2e5", out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryParse_MalformedInput_Fails()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(_parser.TryParse(board, "z9z9", out _), Is.False);
        Assert.That(_parser.TryParse(board, "e2", out _), Is.False);
        Assert.That(_parser.TryParse(board, "", out _), Is.False);
    }

    [Test]
    public void TryParse_PromotionWithExplicitPiece_UsesThatPiece()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

        bool ok = _parser.TryParse(board, "e7e8r", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.Promotion, Is.EqualTo(PieceType.Rook));
    }

    [Test]
    public void TryParse_PromotionWithoutPieceLetter_DefaultsToQueen()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

        bool ok = _parser.TryParse(board, "e7e8", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.Promotion, Is.EqualTo(PieceType.Queen));
    }

    [Test]
    public void TryParse_InvalidPromotionLetter_Fails()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

        Assert.That(_parser.TryParse(board, "e7e8x", out _), Is.False);
    }
}
