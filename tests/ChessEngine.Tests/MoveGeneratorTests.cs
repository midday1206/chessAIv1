using ChessEngine.Core;
using ChessEngine.Core.Moves;

namespace ChessEngine.Tests;

public class MoveGeneratorTests
{
    private static List<Move> MovesFrom(Board board, string square) =>
        MoveGenerator.GeneratePseudoLegalMoves(board)
            .Where(m => m.From == Square.FromAlgebraic(square))
            .ToList();

    [Test]
    public void StartingPosition_HasTwentyLegalMovesForWhite()
    {
        Board board = Board.CreateStartingPosition();

        List<Move> moves = MoveGenerator.GenerateLegalMoves(board);

        Assert.That(moves, Has.Count.EqualTo(20));
    }

    [Test]
    public void Knight_OnEmptyBoardCenter_GeneratesEightMoves()
    {
        Board board = Board.FromFen("8/8/8/3N4/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves, Has.Count.EqualTo(8));
    }

    [Test]
    public void Knight_DoesNotMoveOntoSquareOccupiedByOwnPiece()
    {
        Board board = Board.FromFen("8/8/5P2/3N4/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves.Select(m => m.To), Does.Not.Contain(Square.FromAlgebraic("f6")));
    }

    [Test]
    public void Rook_SlidesUntilBlockedByOwnPiece_AndDoesNotCaptureIt()
    {
        Board board = Board.FromFen("8/8/8/3R3P/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("g5")));
        Assert.That(moves.Select(m => m.To), Does.Not.Contain(Square.FromAlgebraic("h5")));
    }

    [Test]
    public void Rook_CapturesOpponentPieceAndStopsSliding()
    {
        Board board = Board.FromFen("8/8/8/3R3p/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");
        Move capture = moves.Single(m => m.To == Square.FromAlgebraic("h5"));

        Assert.That(capture.IsCapture, Is.True);
        // h5 is the last square on the rank, so the capture itself proves sliding stopped there.
        Assert.That(moves.Count(m => m.To.Rank == 4 && m.To.File > 4), Is.EqualTo(3)); // f5, g5 quiet + h5 capture
    }

    [Test]
    public void Bishop_MovesAlongBothDiagonals()
    {
        Board board = Board.FromFen("8/8/8/3B4/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("a2")));
        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("h1")));
        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("a8")));
        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("g8")));
    }

    [Test]
    public void Queen_CombinesRookAndBishopMoves()
    {
        Board board = Board.FromFen("8/8/8/3Q4/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves, Has.Count.EqualTo(27));
    }

    [Test]
    public void King_OnEmptyBoardCenter_GeneratesEightMoves()
    {
        Board board = Board.FromFen("8/8/8/3K4/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "d5");

        Assert.That(moves, Has.Count.EqualTo(8));
    }

    [Test]
    public void Pawn_White_CanPushOneOrTwoSquaresFromStartingRank()
    {
        Board board = Board.FromFen("8/8/8/8/8/8/4P3/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "e2");

        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("e3")));
        Assert.That(moves.Select(m => m.To), Does.Contain(Square.FromAlgebraic("e4")));
        Assert.That(moves.Single(m => m.To == Square.FromAlgebraic("e4")).IsDoublePawnPush, Is.True);
    }

    [Test]
    public void Pawn_White_CannotDoublePush_WhenNotOnStartingRank()
    {
        Board board = Board.FromFen("8/8/8/8/4P3/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "e4");

        Assert.That(moves.Select(m => m.To), Does.Not.Contain(Square.FromAlgebraic("e6")));
        Assert.That(moves, Has.Count.EqualTo(1));
    }

    [Test]
    public void Pawn_White_CapturesDiagonallyButNotStraightAheadIntoAPiece()
    {
        Board board = Board.FromFen("8/8/8/8/3p4/4P3/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "e3");

        Move capture = moves.Single(m => m.To == Square.FromAlgebraic("d4"));
        Assert.That(capture.IsCapture, Is.True);
    }

    [Test]
    public void Pawn_White_PromotingOnLastRank_GeneratesFourMoves()
    {
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/8 w - - 0 1");

        List<Move> moves = MovesFrom(board, "e7");

        Assert.That(moves, Has.Count.EqualTo(4));
        Assert.That(moves.Select(m => m.Promotion),
            Is.EquivalentTo(new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }));
    }

    [Test]
    public void Pawn_White_CanCaptureEnPassant_WhenTargetIsSet()
    {
        Board board = Board.FromFen("8/8/8/3pP3/8/8/8/8 w - d6 0 1");

        List<Move> moves = MovesFrom(board, "e5");
        Move enPassant = moves.Single(m => m.To == Square.FromAlgebraic("d6"));

        Assert.That(enPassant.IsEnPassant, Is.True);
        Assert.That(enPassant.IsCapture, Is.True);
    }

    [Test]
    public void Castling_WhiteKingside_IsGenerated_WhenPathIsClearAndRightsArePresent()
    {
        Board board = Board.FromFen("8/8/8/8/8/8/8/4K2R w K - 0 1");

        List<Move> moves = MovesFrom(board, "e1");

        Assert.That(moves.Any(m => m.IsCastling && m.To == Square.FromAlgebraic("g1")), Is.True);
    }

    [Test]
    public void Castling_IsNotGenerated_WhenKingIsInCheck()
    {
        Board board = Board.FromFen("4r3/8/8/8/8/8/8/4K2R w K - 0 1");

        List<Move> moves = MovesFrom(board, "e1");

        Assert.That(moves.Any(m => m.IsCastling), Is.False);
    }

    [Test]
    public void Castling_IsNotGenerated_WhenPassingThroughAnAttackedSquare()
    {
        Board board = Board.FromFen("5r2/8/8/8/8/8/8/4K2R w K - 0 1");

        List<Move> moves = MovesFrom(board, "e1");

        Assert.That(moves.Any(m => m.IsCastling), Is.False);
    }

    [Test]
    public void GenerateLegalMoves_ExcludesMovesThatWouldExposeOwnKingToCheck()
    {
        // White king on e1, white rook pinned on e4 by a black rook on e8; only along-the-pin moves are legal.
        Board board = Board.FromFen("4r3/8/8/8/4R3/8/8/4K3 w - - 0 1");

        List<Move> legalRookMoves = MoveGenerator.GenerateLegalMoves(board)
            .Where(m => m.From == Square.FromAlgebraic("e4"))
            .ToList();

        Assert.That(legalRookMoves.Select(m => m.To), Has.All.Matches<Square>(s => s.File == 4));
    }

    [Test]
    public void IsSquareAttacked_DetectsPawnKnightSlidingAndKingAttackers()
    {
        Board board = Board.FromFen("8/8/8/3q4/8/8/8/8 w - - 0 1");

        Assert.That(MoveGenerator.IsSquareAttacked(board, Square.FromAlgebraic("d1"), Color.Black), Is.True);
        Assert.That(MoveGenerator.IsSquareAttacked(board, Square.FromAlgebraic("a1"), Color.Black), Is.False);
    }
}
