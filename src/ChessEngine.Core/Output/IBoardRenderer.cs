namespace ChessEngine.Core.Output;

/// <summary>
/// A port for turning a Board into a presentable form. Implementations are adapters
/// (console text today; a GUI, web, or UCI adapter later) so the presentation layer
/// can be swapped without touching engine logic. Follow the same pattern for any other
/// engine output (move lists, search info, game results, etc.) as they're added.
/// </summary>
public interface IBoardRenderer
{
    string Render(Board board);
}
