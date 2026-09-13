using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChessEngine.Cli;
using ChessEngine.Core;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.Moves;
using ChessEngine.Core.Notation;
using ChessEngine.Core.Output;
using ChessEngine.Core.Search;

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

IMoveSearcher searcher = new MinimaxSearch(new PieceSquareEvaluator());
const int defaultSearchDepth = 4;

// "Pondering": while we're blocked waiting for the next input (the opponent's thinking
// time, from the engine's point of view), keep a search for the CURRENT position running
// on a background thread against its own board clone. If 'go' is later typed for that same
// position at the same depth, we reuse whatever that search already produced instead of
// starting from scratch. Any move being played - by either side - makes the position stale,
// so the next loop iteration cancels it and starts pondering the new position instead.
CancellationTokenSource? ponderCts = null;
Task<Move?>? ponderTask = null;
string? ponderFen = null;
int ponderDepth = -1;

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
    {
        ponderCts?.Cancel();
        return 0;
    }

    StartPondering(board);

    Console.Write("Move (SAN/UCI), 'go [depth]' for the engine to move, 'quit' to exit: ");
    string? input = Console.ReadLine();
    string trimmed = input?.Trim('﻿', ' ', '\t').Trim() ?? "quit";

    if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        ponderCts?.Cancel();
        return 0;
    }

    if (trimmed.Equals("go", StringComparison.OrdinalIgnoreCase) ||
        trimmed.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
    {
        int depth = defaultSearchDepth;
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && int.TryParse(parts[1], out int requestedDepth) && requestedDepth > 0)
            depth = requestedDepth;

        Move? engineMove;
        bool usedPonder = ponderTask is not null && ponderFen == board.ToFen() && ponderDepth == depth;

        if (usedPonder)
        {
            engineMove = ponderTask!.GetAwaiter().GetResult();
        }
        else
        {
            ponderCts?.Cancel();
            engineMove = searcher.FindBestMove(board, depth);
        }

        if (engineMove is null)
        {
            Console.WriteLine("Engine has no legal move.");
            continue;
        }

        string engineSan = moveFormatter.Format(board, engineMove.Value);
        Console.WriteLine($"Engine plays: {engineSan} (depth {depth}{(usedPonder ? ", pondered" : "")})");
        moveHistory.Add(engineSan);
        board.ApplyMove(engineMove.Value);
        continue;
    }

    ponderCts?.Cancel();

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

void StartPondering(Board currentBoard)
{
    ponderCts?.Cancel();

    ponderCts = new CancellationTokenSource();
    CancellationToken token = ponderCts.Token;
    Board snapshot = currentBoard.Clone();

    ponderFen = currentBoard.ToFen();
    ponderDepth = defaultSearchDepth;

    ponderTask = Task.Run(() =>
    {
        try
        {
            return searcher.FindBestMove(snapshot, defaultSearchDepth, token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }, token);
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
