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

    [Test]
    public void IntactPawnShield_ScoresHigherThanAHoleInIt_WithFullMaterial()
    {
        // The g2 shield pawn is relocated to a4 (not removed), so material is identical -
        // only whether the king's own file still has a shield pawn in front of it changes.
        Board intactShield = Board.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1RK1 w kq - 0 1");
        Board holedShield = Board.FromFen("rnbqkbnr/pppppppp/8/8/P7/8/PPPPPP1P/RNBQ1RK1 w kq - 0 1");

        Assert.That(_evaluator.Evaluate(intactShield), Is.GreaterThan(_evaluator.Evaluate(holedShield)));
    }

    [Test]
    public void PawnShieldHole_CostsFarLess_InABareEndgame()
    {
        // Same shield hole (g2 relocated to a4) as above, but compare how much it costs with
        // full material still on the board versus a bare king+pawn ending (phase 0) - king
        // safety should fade out almost entirely once there's little left to exploit it with.
        Board middlegameIntact = Board.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQ1RK1 w kq - 0 1");
        Board middlegameHoled = Board.FromFen("rnbqkbnr/pppppppp/8/8/P7/8/PPPPPP1P/RNBQ1RK1 w kq - 0 1");
        Board endgameIntact = Board.FromFen("4k3/8/8/8/8/8/5PPP/6K1 w - - 0 1");
        Board endgameHoled = Board.FromFen("4k3/8/8/8/P7/8/5P1P/6K1 w - - 0 1");

        int middlegameGap = _evaluator.Evaluate(middlegameIntact) - _evaluator.Evaluate(middlegameHoled);
        int endgameGap = _evaluator.Evaluate(endgameIntact) - _evaluator.Evaluate(endgameHoled);

        Assert.That(middlegameGap, Is.GreaterThan(endgameGap));
    }

    [Test]
    public void PawnStorm_OnTheWingAwayFromItsOwnKing_ScoresHigherThanPushingInFrontOfIt()
    {
        // The pawn table itself is left-right symmetric, so b4 and g4 carry the same raw
        // placement bonus - only the storm bonus (queenside push while castled kingside)
        // should be able to separate these two boards.
        Board pushesAwayFromKing = Board.FromFen("rnbqkbnr/pppppppp/8/8/1P6/8/P1PPPPPP/RNBQ1RK1 w kq - 0 1");
        Board pushesInFrontOfKing = Board.FromFen("rnbqkbnr/pppppppp/8/8/6P1/8/PPPPPP1P/RNBQ1RK1 w kq - 0 1");

        Assert.That(_evaluator.Evaluate(pushesAwayFromKing), Is.GreaterThan(_evaluator.Evaluate(pushesInFrontOfKing)));
    }
}
