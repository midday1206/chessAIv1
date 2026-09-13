using ChessEngine.Cli;
using ChessEngine.Core;
using ChessEngine.Core.Moves;
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

IBoardRenderer boardRenderer = new TextBoardRenderer();
IGameStatusRenderer statusRenderer = new TextGameStatusRenderer();
GameStatus status = MoveGenerator.GetGameStatus(board);

Console.WriteLine(boardRenderer.Render(board));
Console.WriteLine();
Console.WriteLine($"FEN: {board.ToFen()}");
Console.WriteLine(statusRenderer.Render(board, status));

return 0;
