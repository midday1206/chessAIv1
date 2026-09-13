using ChessEngine.Cli;
using ChessEngine.Core;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;
using ChessEngine.Core.Output;

string startingFen = args.Length > 0 ? string.Join(' ', args) : Board.StartingFen;

Board board;
try
{
    board = Board.FromFen(startingFen);
}
catch (Exception ex) when (ex is FormatException or ArgumentException)
{
    Console.Error.WriteLine($"Invalid FEN: {ex.Message}");
    return 1;
}

IBoardRenderer boardRenderer = new TextBoardRenderer();
IGameStatusRenderer statusRenderer = new TextGameStatusRenderer();
IMoveParser moveParser = new UciMoveParser();

while (true)
{
    GameStatus status = MoveGenerator.GetGameStatus(board);

    Console.WriteLine();
    Console.WriteLine(boardRenderer.Render(board));
    Console.WriteLine();
    Console.WriteLine($"FEN: {board.ToFen()}");
    Console.WriteLine(statusRenderer.Render(board, status));

    if (status is GameStatus.Checkmate or GameStatus.Stalemate)
        return 0;

    Console.Write("Move (e.g. e2e4, e7e8q; 'quit' to exit): ");
    string? input = Console.ReadLine();
    string trimmed = input?.Trim('﻿', ' ', '\t').Trim() ?? "quit";

    if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        return 0;
    }

    if (!moveParser.TryParse(board, trimmed, out Move move))
    {
        Console.WriteLine($"'{trimmed}' is not a legal move.");
        continue;
    }

    board.ApplyMove(move);
}
