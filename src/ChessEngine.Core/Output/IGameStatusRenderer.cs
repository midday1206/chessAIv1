namespace ChessEngine.Core.Output;

/// <summary>A port for presenting a position's GameStatus (check/checkmate/stalemate/in progress).</summary>
public interface IGameStatusRenderer
{
    string Render(Board board, GameStatus status);
}
