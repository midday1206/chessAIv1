using ChessEngine.Cli;
using ChessEngine.Core;
using ChessEngine.Core.Output;

string fen = args.Length > 0 ? string.Join(' ', args) : Board.StartingFen;

Board board;
try
{
    board = Board.FromFen(fen);
}
catch (Exception ex) when (ex is FormatException or ArgumentException)
{
    Console.Error.WriteLine($"Invalid FEN: {ex.Message}");
    return 1;
}

IBoardRenderer renderer = new TextBoardRenderer();
Console.WriteLine(renderer.Render(board));
Console.WriteLine();
Console.WriteLine($"FEN: {board.ToFen()}");

return 0;
