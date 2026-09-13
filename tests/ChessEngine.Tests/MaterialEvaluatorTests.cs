using ChessEngine.Core;
using ChessEngine.Core.Evaluation;

namespace ChessEngine.Tests;

public class MaterialEvaluatorTests
{
    private readonly MaterialEvaluator _evaluator = new();

    [Test]
    public void StartingPosition_IsBalanced()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(_evaluator.Evaluate(board), Is.EqualTo(0));
    }

    [Test]
    public void WhiteUpAQueen_IsStronglyPositive()
    {
        Board board = Board.FromFen("4k3/8/8/8/8/8/8/3QK3 w - - 0 1");

        Assert.That(_evaluator.Evaluate(board), Is.EqualTo(900));
    }

    [Test]
    public void BlackUpARook_IsNegative()
    {
        Board board = Board.FromFen("3rk3/8/8/8/8/8/8/4K3 w - - 0 1");

        Assert.That(_evaluator.Evaluate(board), Is.EqualTo(-500));
    }
}
