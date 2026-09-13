namespace ChessEngine.Core;

public enum Color
{
    White,
    Black
}

public static class ColorExtensions
{
    public static Color Opposite(this Color color) =>
        color == Color.White ? Color.Black : Color.White;
}
