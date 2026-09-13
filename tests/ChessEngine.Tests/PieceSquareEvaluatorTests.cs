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
}
