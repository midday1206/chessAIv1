using ChessEngine.Core;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Search;

namespace ChessEngine.Tests;

public class MinimaxSearchTests
{
    private readonly MinimaxSearch _search = new(new MaterialEvaluator());

    [Test]
    public void FindBestMove_PlaysAnAvailableCheckmateInOne()
    {
        // 1. f3 e5 2. g4 - Black to move has Qh4# available.
        Board board = Board.FromFen("rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 2");

        Move? move = _search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.From, Is.EqualTo(Square.FromAlgebraic("d8")));
        Assert.That(move.Value.To, Is.EqualTo(Square.FromAlgebraic("h4")));
    }

    [Test]
    public void FindBestMove_ReturnsNull_WhenAlreadyCheckmated()
    {
        Board board = Board.FromFen("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3");

        Move? move = _search.FindBestMove(board, depth: 3);

        Assert.That(move, Is.Null);
    }

    [Test]
    public void FindBestMove_TakesAFreeUndefendedPiece()
    {
        Board board = Board.FromFen("4k3/8/8/3n4/4P3/8/8/4K3 w - - 0 1");

        Move? move = _search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.From, Is.EqualTo(Square.FromAlgebraic("e4")));
        Assert.That(move.Value.To, Is.EqualTo(Square.FromAlgebraic("d5")));
    }

    [Test]
    public void FindBestMove_AvoidsACaptureThatLosesTheQueenForLess()
    {
        // Qd1 could take the knight on d5, but a pawn on e6 recaptures for a net loss.
        Board board = Board.FromFen("k7/8/4p3/3n4/8/8/P7/K2Q4 w - - 0 1");

        Move? move = _search.FindBestMove(board, depth: 3);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.From == Square.FromAlgebraic("d1") && move.Value.To == Square.FromAlgebraic("d5"),
            Is.False, "the engine should not trade its queen for a defended knight");
    }

    [Test]
    public void FindBestMove_Throws_WhenDepthIsLessThanOne()
    {
        Board board = Board.CreateStartingPosition();

        Assert.Throws<ArgumentOutOfRangeException>(() => _search.FindBestMove(board, depth: 0));
    }
}
