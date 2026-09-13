using ChessEngine.Core;
using ChessEngine.Core.Moves;

namespace ChessEngine.Tests;

public class GameStatusTests
{
    [Test]
    public void StartingPosition_IsInProgress()
    {
        Board board = Board.CreateStartingPosition();

        Assert.That(MoveGenerator.GetGameStatus(board), Is.EqualTo(GameStatus.InProgress));
        Assert.That(MoveGenerator.IsInCheck(board, Color.White), Is.False);
    }

    [Test]
    public void FoolsMate_IsCheckmateForWhite()
    {
        // 1. f3 e5 2. g4 Qh4# - the fastest possible checkmate.
        Board board = Board.FromFen("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3");

        Assert.That(MoveGenerator.IsInCheck(board, Color.White), Is.True);
        Assert.That(MoveGenerator.GetGameStatus(board), Is.EqualTo(GameStatus.Checkmate));
        Assert.That(MoveGenerator.GenerateLegalMoves(board), Is.Empty);
    }

    [Test]
    public void ClassicPosition_IsStalemate()
    {
        // Black king h8 boxed in by the white queen/king, not in check, and with no legal move.
        Board board = Board.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");

        Assert.That(MoveGenerator.IsInCheck(board, Color.Black), Is.False);
        Assert.That(MoveGenerator.GetGameStatus(board), Is.EqualTo(GameStatus.Stalemate));
        Assert.That(MoveGenerator.GenerateLegalMoves(board), Is.Empty);
    }

    [Test]
    public void KingInCheckWithAnEscapeSquare_IsCheckNotCheckmate()
    {
        // White rook checks the black king along the e-file, but the king can step aside to d7.
        Board board = Board.FromFen("4k3/8/8/8/8/8/8/4R1K1 b - - 0 1");

        Assert.That(MoveGenerator.IsInCheck(board, Color.Black), Is.True);
        Assert.That(MoveGenerator.GetGameStatus(board), Is.EqualTo(GameStatus.Check));
        Assert.That(MoveGenerator.GenerateLegalMoves(board), Is.Not.Empty);
    }
}
