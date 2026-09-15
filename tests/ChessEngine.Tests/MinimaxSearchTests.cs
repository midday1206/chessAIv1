using System.Threading;
using ChessEngine.Core;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;
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

    [Test]
    public void FindBestMove_Throws_WhenCancelledBeforeStarting()
    {
        Board board = Board.CreateStartingPosition();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => _search.FindBestMove(board, depth: 4, cts.Token));
    }

    [Test]
    public void FindBestMove_PrefersAQuietMove_OverAnEquallyEvaluatedTrade()
    {
        // Rxa6 bxa6 is a perfectly even rook trade - the material balance afterward is
        // identical to just leaving the position alone - so the engine should keep the
        // tension on the board instead of simplifying for no actual gain.
        Board board = Board.FromFen("4k3/1p6/r7/8/8/8/8/R3K3 w - - 0 1");

        Move? move = _search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.IsCapture, Is.False);
    }

    [Test]
    public void FindBestMove_StillTakesAClearlyWinningTrade()
    {
        // An undefended knight is a real material gain, not a near-tie, so the trade-avoidance
        // tie-break must not suppress it.
        Board board = Board.FromFen("4k3/8/n7/8/8/8/8/R3K3 w - - 0 1");

        Move? move = _search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.To, Is.EqualTo(Square.FromAlgebraic("a6")));
        Assert.That(move.Value.IsCapture, Is.True);
    }

    [Test]
    public void FindBestMove_AtShallowDepth_StillAvoidsABadTradeThanksToQuiescence()
    {
        // Same trap as FindBestMove_AvoidsACaptureThatLosesTheQueenForLess, but searched to
        // only 1 ply. Without quiescence, the leaf evaluation right after Qxd5 looks great
        // (queen just won a knight) and the search would never see the pawn recapture -
        // exactly the horizon effect quiescence search exists to fix.
        Board board = Board.FromFen("k7/8/4p3/3n4/8/8/P7/K2Q4 w - - 0 1");

        Move? move = _search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        Assert.That(move!.Value.From == Square.FromAlgebraic("d1") && move.Value.To == Square.FromAlgebraic("d5"),
            Is.False, "quiescence search should see the pawn recapture even at depth 1");
    }

    [Test]
    public void FindBestMove_WithPieceSquareEvaluator_PrefersDevelopmentOverAimlessFlankPawnPushes()
    {
        // With material alone every quiet opening move ties at depth 1, so the engine has no
        // reason to prefer developing a piece over pushing a rim pawn. Piece-square tables
        // should break that tie in favor of central/development moves.
        var search = new MinimaxSearch(new PieceSquareEvaluator());
        Board board = Board.CreateStartingPosition();

        Move? move = search.FindBestMove(board, depth: 1);

        Assert.That(move, Is.Not.Null);
        string san = new SanMoveFormatter().Format(board, move!.Value);
        Assert.That(san, Is.Not.AnyOf("a3", "a4", "h3", "h4"));
    }
}
