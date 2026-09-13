using ChessEngine.Core;

namespace ChessEngine.Tests;

public class BoardTests
{
    [Test]
    public void CreateStartingPosition_PlacesWhiteRookOnA1()
    {
        Board board = Board.CreateStartingPosition();

        Piece piece = board.GetPiece(Square.FromAlgebraic("a1"));

        Assert.That(piece.Type, Is.EqualTo(PieceType.Rook));
        Assert.That(piece.Color, Is.EqualTo(Color.White));
    }

    [Test]
    public void CreateStartingPosition_PlacesBlackKingOnE8()
    {
        Board board = Board.CreateStartingPosition();

        Piece piece = board.GetPiece(Square.FromAlgebraic("e8"));

        Assert.That(piece.Type, Is.EqualTo(PieceType.King));
        Assert.That(piece.Color, Is.EqualTo(Color.Black));
    }

    [Test]
    public void CreateStartingPosition_HasEmptySquaresInTheMiddle()
    {
        Board board = Board.CreateStartingPosition();

        Piece piece = board.GetPiece(Square.FromAlgebraic("e4"));

        Assert.That(piece.IsNone, Is.True);
    }

    [Test]
    public void FromFen_ThenToFen_RoundTripsTheStartingPosition()
    {
        Board board = Board.FromFen(Board.StartingFen);

        Assert.That(board.ToFen(), Is.EqualTo(Board.StartingFen));
    }

    [Test]
    public void FromFen_ParsesSideToMoveAndCastlingAndEnPassant()
    {
        const string fen = "rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR b KQkq c6 0 2";

        Board board = Board.FromFen(fen);

        Assert.That(board.SideToMove, Is.EqualTo(Color.Black));
        Assert.That(board.Castling, Is.EqualTo(CastlingRights.All));
        Assert.That(board.EnPassantTarget, Is.EqualTo(Square.FromAlgebraic("c6")));
        Assert.That(board.HalfmoveClock, Is.EqualTo(0));
        Assert.That(board.FullmoveNumber, Is.EqualTo(2));
    }

    [Test]
    public void FromFen_WithNoCastlingRights_ParsesDash()
    {
        Board board = Board.FromFen("8/8/8/8/8/8/8/8 w - - 0 1");

        Assert.That(board.Castling, Is.EqualTo(CastlingRights.None));
        Assert.That(board.EnPassantTarget, Is.Null);
    }

    [Test]
    public void FromFen_WithWrongRankCount_Throws()
    {
        Assert.Throws<FormatException>(() => Board.FromFen("8/8/8 w - - 0 1"));
    }

    [Test]
    public void FromFen_WithInvalidPieceCharacter_Throws()
    {
        Assert.Throws<FormatException>(() => Board.FromFen("8/8/8/8/8/8/8/7z w - - 0 1"));
    }
}
