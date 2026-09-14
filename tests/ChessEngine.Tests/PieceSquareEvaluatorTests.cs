using ChessEngine.Core;
using ChessEngine.Core.Evaluation;

namespace ChessEngine.Tests;

public class PieceSquareEvaluatorTests
{
    private readonly PieceSquareEvaluator _evaluator = new();

    [Test]
    public void StartingPosition_IsBalanced()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(_evaluator.Evaluate(board), Is.EqualTo(0));
    }

    [Test]
    public void KnightInTheCenter_ScoresHigherThanKnightInTheCorner()
    {
        Board centerBoard = Board.FromFen("8/8/8/4N3/8/8/8/8 w - - 0 1");
        Board cornerBoard = Board.FromFen("8/8/8/8/8/8/8/N7 w - - 0 1");

        Assert.That(_evaluator.Evaluate(centerBoard), Is.GreaterThan(_evaluator.Evaluate(cornerBoard)));
    }

    [Test]
    public void AdvancedPawn_ScoresHigherThanItsStartingSquare_WithSameMaterial()
    {
        Board advanced = Board.FromFen("8/8/8/8/4P3/8/8/8 w - - 0 1");
        Board home = Board.FromFen("8/8/8/8/8/8/4P3/8 w - - 0 1");

        Assert.That(_evaluator.Evaluate(advanced), Is.GreaterThan(_evaluator.Evaluate(home)));
    }

    [Test]
    public void TablesAreMirroredForBlack()
    {
        // A white knight on d4 and a black knight on d5 sit on each other's mirror image
        // square, so with nothing else on the board the position should be exactly balanced.
        Board board = Board.FromFen("8/8/8/3n4/3N4/8/8/8 w - - 0 1");

        Assert.That(_evaluator.Evaluate(board), Is.EqualTo(0));
    }

    [Test]
    public void CastledKing_ScoresHigherThanUncastledKing_InTheMiddlegame()
    {
        Board castled = Board.FromFen("8/8/8/8/8/8/8/5RK1 w - - 0 1");
        Board uncastled = Board.FromFen("8/8/8/8/8/8/8/4K2R w - - 0 1");

        Assert.That(_evaluator.Evaluate(castled), Is.GreaterThan(_evaluator.Evaluate(uncastled)));
    }

    [Test]
    public void King_PrefersStayingBack_WithFullNonPawnMaterialOnTheBoard()
    {
        Board kingOnBackRank = Board.FromFen("rnbqkbnr/8/8/8/8/8/8/RNBQKBNR w - - 0 1");
        Board kingInTheCenter = Board.FromFen("rnbqkbnr/8/8/8/4K3/8/8/RNBQ1BNR w - - 0 1");

        Assert.That(_evaluator.Evaluate(kingOnBackRank), Is.GreaterThan(_evaluator.Evaluate(kingInTheCenter)));
    }

    [Test]
    public void King_PrefersCentralizing_InABareEndgame()
    {
        Board kingOnBackRank = Board.FromFen("8/8/8/8/8/8/8/4K3 w - - 0 1");
        Board kingInTheCenter = Board.FromFen("8/8/8/8/4K3/8/8/8 w - - 0 1");

        Assert.That(_evaluator.Evaluate(kingInTheCenter), Is.GreaterThan(_evaluator.Evaluate(kingOnBackRank)));
    }

    [Test]
    public void PassedPawn_OutweighsATableDisadvantage_ComparedToABlockedPawn()
    {
        // A black pawn fixed on e6 in both boards (its own contribution cancels out): it
        // directly blocks a white pawn on e5, but not one on a5, which is passed instead.
        Board blockedOnE5 = Board.FromFen("8/8/4p3/4P3/8/8/8/8 w - - 0 1");
        Board passedOnA5 = Board.FromFen("8/8/4p3/P7/8/8/8/8 w - - 0 1");

        Assert.That(_evaluator.Evaluate(passedOnA5), Is.GreaterThan(_evaluator.Evaluate(blockedOnE5)));
    }
}
