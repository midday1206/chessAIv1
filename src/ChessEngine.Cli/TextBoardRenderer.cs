using System.Text;
using ChessEngine.Core;
using ChessEngine.Core.Output;

namespace ChessEngine.Cli;

/// <summary>Console adapter for IBoardRenderer: renders a Board as a plain-text grid.</summary>
public sealed class TextBoardRenderer : IBoardRenderer
{
    public string Render(Board board)
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
