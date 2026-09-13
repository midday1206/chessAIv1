using ChessEngine.Core;

namespace ChessEngine.Tests;

public class SquareTests
{
    [TestCase("a1", 0, 0)]
    [TestCase("h8", 7, 7)]
    [TestCase("e4", 4, 3)]
    public void FromAlgebraic_ParsesFileAndRank(string algebraic, int expectedFile, int expectedRank)
    {
        Square square = Square.FromAlgebraic(algebraic);

        Assert.That(square.File, Is.EqualTo(expectedFile));
        Assert.That(square.Rank, Is.EqualTo(expectedRank));
    }

    [TestCase("z9")]
    [TestCase("a9")]
    [TestCase("i1")]
    [TestCase("a0")]
    [TestCase("abc")]
    [TestCase("")]
    public void FromAlgebraic_WithInvalidInput_ThrowsFormatException(string algebraic)
    {
        Assert.Throws<FormatException>(() => Square.FromAlgebraic(algebraic));
    }

    [Test]
    public void ToAlgebraic_RoundTripsWithFromAlgebraic()
    {
        var square = new Square(3, 6);

        Assert.That(Square.FromAlgebraic(square.ToAlgebraic()), Is.EqualTo(square));
    }
}
