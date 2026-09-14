using ChessEngine.Core;
using ChessEngine.Core.Evaluation;

namespace ChessEngine.Tests;

public class GamePhaseTests
{
    [Test]
    public void FullNonPawnMaterial_HasZeroEndgameWeight()
    {
        Board board = Board.FromFen("rnbqkbnr/8/8/8/8/8/8/RNBQKBNR w - - 0 1");

        Assert.That(GamePhase.Compute(board), Is.EqualTo(GamePhase.TotalPhase));
        Assert.That(GamePhase.EndgameWeight(board), Is.EqualTo(0.0));
    }

    [Test]
    public void BareKings_HasFullEndgameWeight()
    {
        Board board = Board.FromFen("4k3/8/8/8/8/8/8/4K3 w - - 0 1");

        Assert.That(GamePhase.Compute(board), Is.EqualTo(0));
        Assert.That(GamePhase.EndgameWeight(board), Is.EqualTo(1.0));
    }

    [Test]
    public void Compute_WeighsQueensAndRooksMoreThanMinorPieces()
    {
        Board queenAndRook = Board.FromFen("4k3/8/8/8/8/8/8/3RK2Q w - - 0 1");

        // 1 rook (2) + 1 queen (4) = 6.
        Assert.That(GamePhase.Compute(queenAndRook), Is.EqualTo(6));
    }

    [Test]
    public void Compute_IgnoresPawnsAndKings()
    {
        Board board = Board.FromFen("4k3/pppppppp/8/8/8/8/PPPPPPPP/4K3 w - - 0 1");

        Assert.That(GamePhase.Compute(board), Is.EqualTo(0));
    }
}
