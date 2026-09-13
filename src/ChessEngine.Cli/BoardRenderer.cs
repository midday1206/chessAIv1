using System.Text;
using ChessEngine.Core;

namespace ChessEngine.Cli;

/// <summary>Renders a Board as a human-readable text grid for the console.</summary>
public static class BoardRenderer
{
    public static string Render(Board board)
    {
        var sb = new StringBuilder();

        for (int rank = Board.BoardSize - 1; rank >= 0; rank--)
        {
            sb.Append(rank + 1).Append(' ').Append(' ');

            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                sb.Append(piece.IsNone ? '.' : piece.ToFenChar()).Append(' ');
            }

            sb.AppendLine();
        }

        sb.Append("   ");
        for (char f = 'a'; f <= 'h'; f++)
            sb.Append(f).Append(' ');

        return sb.ToString().TrimEnd();
    }
}
