using ChessEngine.Core;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;

namespace ChessEngine.Tests;

public class SanMoveFormatterTests
{
    private readonly SanMoveFormatter _formatter = new();

    private static Move LegalMove(Board board, string from, string to, PieceType promotion = PieceType.None)
    {
        Square f = Square.FromAlgebraic(from);
        Square t = Square.FromAlgebraic(to);
        return MoveGenerator.GenerateLegalMoves(board)
            .Single(m => m.From == f && m.To == t && m.Promotion == promotion);
    }

    [Test]
    public void PawnPush_HasNoPieceLetter()
    {
        Board board = Board.CreateStartingPosition();

        string san = _formatter.Format(board, LegalMove(board, "e2", "e4"));

        Assert.That(san, Is.EqualTo("e4"));
    }

    [Test]
    public void PawnCapture_IsPrefixedWithSourceFile()
    {
        Board board = Board.FromFen("4k3/8/8/8/3p4/4P3/8/4K3 w - - 0 1");

        string san = _formatter.Format(board, LegalMove(board, "e3", "d4"));

        Assert.That(san, Is.EqualTo("exd4"));
    }

    [Test]
    public void KnightMove_UsesPieceLetterAndNoDisambiguationWhenUnambiguous()
    {
        Board board = Board.CreateStartingPosition();

        string san = _formatter.Format(board, LegalMove(board, "g1", "f3"));

        Assert.That(san, Is.EqualTo("Nf3"));
    }

    [Test]
    public void KnightMove_DisambiguatesByFile_WhenTwoKnightsShareARankAndTarget()
    {
        Board board = Board.FromFen("6k1/8/8/8/8/8/8/1N3NK1 w - - 0 1");

        string sanFromB1 = _formatter.Format(board, LegalMove(board, "b1", "d2"));
        string sanFromF1 = _formatter.Format(board, LegalMove(board, "f1", "d2"));

        Assert.That(sanFromB1, Is.EqualTo("Nbd2"));
        Assert.That(sanFromF1, Is.EqualTo("Nfd2"));
    }

    [Test]
    public void RookMove_DisambiguatesByRank_WhenTwoRooksShareAFileAndTarget()
    {
        // Black king sits off both rooks' lines (not on the a-file, rank 1, or rank 8)
        // so the tested move itself carries no incidental check.
        Board board = Board.FromFen("R7/8/8/4k3/8/8/8/R5K1 w - - 0 1");

        string sanFromA1 = _formatter.Format(board, LegalMove(board, "a1", "a4"));
        string sanFromA8 = _formatter.Format(board, LegalMove(board, "a8", "a4"));

        Assert.That(sanFromA1, Is.EqualTo("R1a4"));
        Assert.That(sanFromA8, Is.EqualTo("R8a4"));
    }

    [Test]
    public void Castling_IsFormattedAsOO()
    {
        Board board = Board.FromFen("k7/8/8/8/8/8/8/4K2R w K - 0 1");

        string san = _formatter.Format(board, LegalMove(board, "e1", "g1"));

        Assert.That(san, Is.EqualTo("O-O"));
    }

    [Test]
    public void CastlingQueenside_IsFormattedAsOOO()
    {
        Board board = Board.FromFen("7k/8/8/8/8/8/8/R3K3 w Q - 0 1");

        string san = _formatter.Format(board, LegalMove(board, "e1", "c1"));

        Assert.That(san, Is.EqualTo("O-O-O"));
    }

    [Test]
    public void Promotion_AppendsEqualsPieceLetter()
    {
        // Black king is off the e-file so the promotion itself doesn't incidentally check it.
        Board board = Board.FromFen("8/4P3/8/8/8/8/8/k6K w - - 0 1");

        string san = _formatter.Format(board, LegalMove(board, "e7", "e8", PieceType.Queen));

        Assert.That(san, Is.EqualTo("e8=Q"));
    }

    [Test]
    public void MoveThatGivesCheck_HasPlusSuffix()
    {
        Board board = Board.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1");

        string san = _formatter.Format(board, LegalMove(board, "a1", "a8"));

        Assert.That(san, Is.EqualTo("Ra8+"));
    }

    [Test]
    public void FoolsMateFinalMove_IsFormattedAsCheckmate()
    {
        Board board = Board.FromFen("rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 2");

        string san = _formatter.Format(board, LegalMove(board, "d8", "h4"));

        Assert.That(san, Is.EqualTo("Qh4#"));
    }
}
