using ChessEngine.Core;

namespace ChessEngine.Tests;

public class PieceTests
{
    [TestCase(PieceType.Pawn, Color.White, 'P')]
    [TestCase(PieceType.Knight, Color.White, 'N')]
    [TestCase(PieceType.King, Color.Black, 'k')]
    [TestCase(PieceType.Queen, Color.Black, 'q')]
    public void ToFenChar_MatchesExpectedCasing(PieceType type, Color color, char expected)
    {
        var piece = new Piece(type, color);

        Assert.That(piece.ToFenChar(), Is.EqualTo(expected));
    }

    [TestCase('P', PieceType.Pawn, Color.White)]
    [TestCase('n', PieceType.Knight, Color.Black)]
    [TestCase('R', PieceType.Rook, Color.White)]
    [TestCase('q', PieceType.Queen, Color.Black)]
    public void FromFenChar_ParsesTypeAndColor(char c, PieceType expectedType, Color expectedColor)
    {
        Piece piece = Piece.FromFenChar(c);

        Assert.That(piece.Type, Is.EqualTo(expectedType));
        Assert.That(piece.Color, Is.EqualTo(expectedColor));
    }

    [Test]
    public void FromFenChar_WithUnknownLetter_Throws()
    {
        Assert.Throws<FormatException>(() => Piece.FromFenChar('x'));
    }

    [Test]
    public void None_IsNone()
    {
        Assert.That(Piece.None.IsNone, Is.True);
    }
}
