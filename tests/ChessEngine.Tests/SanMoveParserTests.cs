using ChessEngine.Core;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;

namespace ChessEngine.Tests;

public class SanMoveParserTests
{
    private readonly SanMoveParser _parser = new();

    [Test]
    public void TryParse_PawnPush_Succeeds()
    {
        Board board = Board.CreateStartingPosition();

        bool ok = _parser.TryParse(board, "e4", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("e2")));
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("e4")));
    }

    [Test]
    public void TryParse_KnightMove_Succeeds()
    {
        Board board = Board.CreateStartingPosition();

        bool ok = _parser.TryParse(board, "Nf3", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("g1")));
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("f3")));
    }

    [Test]
    public void TryParse_PawnCapture_ReadsSourceFileFromPrefix()
    {
        Board board = Board.FromFen("4k3/8/8/8/3p4/4P3/8/4K3 w - - 0 1");

        bool ok = _parser.TryParse(board, "exd4", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("e3")));
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("d4")));
        Assert.That(move.IsCapture, Is.True);
    }

    [Test]
    public void TryParse_DisambiguatedKnightMove_PicksTheRequestedOrigin()
    {
        Board board = Board.FromFen("6k1/8/8/8/8/8/8/1N3NK1 w - - 0 1");

        bool ok = _parser.TryParse(board, "Nbd2", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("b1")));
    }

    [Test]
    public void TryParse_RankDisambiguatedRookMove_PicksTheRequestedOrigin()
    {
        Board board = Board.FromFen("R6k/8/8/8/8/8/8/R3K3 w - - 0 1");

        bool ok = _parser.TryParse(board, "R8a4", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.From, Is.EqualTo(Square.FromAlgebraic("a8")));
    }

    [TestCase("O-O")]
    [TestCase("0-0")]
    [TestCase("o-o")]
    public void TryParse_KingsideCastling_Succeeds(string input)
    {
        Board board = Board.FromFen("8/8/8/8/8/8/8/4K2R w K - 0 1");

        bool ok = _parser.TryParse(board, input, out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.IsCastling, Is.True);
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("g1")));
    }

    [Test]
    public void TryParse_QueensideCastling_Succeeds()
    {
        Board board = Board.FromFen("8/8/8/8/8/8/8/R3K3 w Q - 0 1");

        bool ok = _parser.TryParse(board, "O-O-O", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("c1")));
    }

    [Test]
    public void TryParse_PromotionWithExplicitPiece_UsesThatPiece()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

        bool ok = _parser.TryParse(board, "e8=R", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.Promotion, Is.EqualTo(PieceType.Rook));
    }

    [Test]
    public void TryParse_PromotionWithoutPieceLetter_DefaultsToQueen()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/4k2K w - - 0 1");

        bool ok = _parser.TryParse(board, "e8", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.Promotion, Is.EqualTo(PieceType.Queen));
    }

    [Test]
    public void TryParse_IgnoresTrailingCheckAndMateMarkers()
    {
        Board board = Board.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1");

        bool ok = _parser.TryParse(board, "Ra8+", out Move move);

        Assert.That(ok, Is.True);
        Assert.That(move.To, Is.EqualTo(Square.FromAlgebraic("a8")));
    }

    [Test]
    public void TryParse_IllegalMove_Fails()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(_parser.TryParse(board, "Nf6", out _), Is.False);
    }

    [Test]
    public void TryParse_GarbageInput_Fails()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(_parser.TryParse(board, "not a move", out _), Is.False);
        Assert.That(_parser.TryParse(board, "", out _), Is.False);
    }
}
