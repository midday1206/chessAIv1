using ChessEngine.Core;
using ChessEngine.Core.Output;

namespace ChessEngine.Cli;

/// <summary>Console adapter for IGameStatusRenderer: renders GameStatus as a plain-text line.</summary>
public sealed class TextGameStatusRenderer : IGameStatusRenderer
{
    public string Render(Board board, GameStatus status)
    {
        string side = board.SideToMove == Color.White ? "White" : "Black";
        string winner = board.SideToMove == Color.White ? "Black" : "White";

        return status switch
        {
            GameStatus.Checkmate => $"Checkmate - {winner} wins.",
            GameStatus.Stalemate => "Stalemate - draw.",
            GameStatus.Check => $"{side} is in check.",
            _ => $"{side} to move."
        };
    }
}
