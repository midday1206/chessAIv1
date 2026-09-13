using System.Text;
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
IMoveFormatter moveFormatter = new SanMoveFormatter();

// Tried in order: SAN ("Nf3", "exd5", "O-O") first, then UCI long algebraic ("g1f3") as a fallback.
IMoveParser[] moveParsers = { new SanMoveParser(), new UciMoveParser() };

Color openingSide = board.SideToMove;
int openingFullmove = board.FullmoveNumber;
var moveHistory = new List<string>();

while (true)
{
    GameStatus status = MoveGenerator.GetGameStatus(board);

    Console.WriteLine();
    Console.WriteLine(boardRenderer.Render(board));
    Console.WriteLine();
    if (moveHistory.Count > 0)
        Console.WriteLine(FormatMoveText(moveHistory, openingSide, openingFullmove));
    Console.WriteLine($"FEN: {board.ToFen()}");
    Console.WriteLine(statusRenderer.Render(board, status));

    if (status is GameStatus.Checkmate or GameStatus.Stalemate)
        return 0;

    Console.Write("Move (SAN like Nf3, or UCI like g1f3; 'quit' to exit): ");
    string? input = Console.ReadLine();
    string trimmed = input?.Trim('﻿', ' ', '\t').Trim() ?? "quit";

    if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        return 0;
    }

    Move? parsedMove = null;
    foreach (IMoveParser parser in moveParsers)
    {
        if (parser.TryParse(board, trimmed, out Move candidate))
        {
            parsedMove = candidate;
            break;
        }
    }

    if (parsedMove is null)
    {
        Console.WriteLine($"'{trimmed}' is not a legal move.");
        continue;
    }

    moveHistory.Add(moveFormatter.Format(board, parsedMove.Value));
    board.ApplyMove(parsedMove.Value);
}

static string FormatMoveText(List<string> history, Color startingSide, int startingFullmove)
{
    var sb = new StringBuilder();
    int fullmove = startingFullmove;
    bool isWhiteTurn = startingSide == Color.White;

    for (int i = 0; i < history.Count; i++)
    {
        if (isWhiteTurn)
        {
            sb.Append(fullmove).Append(". ").Append(history[i]).Append(' ');
        }
        else
        {
            if (i == 0) sb.Append(fullmove).Append("... ");
            sb.Append(history[i]).Append(' ');
            fullmove++;
        }

        isWhiteTurn = !isWhiteTurn;
    }

    return sb.ToString().TrimEnd();
}
