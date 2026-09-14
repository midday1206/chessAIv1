using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChessEngine.Cli;
using ChessEngine.Core;
using ChessEngine.Core.Book;
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

// As material comes off the board the branching factor shrinks a lot, and endgame technique
// (king activity, opposition, pawn races) often needs more than a few plies of lookahead to
// get right - so search noticeably deeper once the position has simplified, for the same
// reason a fixed 4-ply budget is fine in the middlegame but not once it's down to king + pawns.
int SelectSearchDepth(Board b)
{
    int phase = GamePhase.Compute(b);
    if (phase <= 6) return defaultSearchDepth + 4;
    if (phase <= 12) return defaultSearchDepth + 2;
    return defaultSearchDepth;
}

// Opening book: not "whatever was played" - after the game ends, every position from the
// opening phase (the first bookMaxPlies plies) gets re-analyzed at bookAnalysisDepth (deeper
// than normal play) and THAT best move is what's stored. Playing/teaching a line once and
// then quitting is enough to pin it as the repertoire for next time.
const int bookMaxPlies = 20;
const int bookAnalysisDepth = defaultSearchDepth + 2;
string bookPath = Path.Combine(Directory.GetCurrentDirectory(), "opening-book.json");
OpeningBook openingBook = OpeningBook.Load(bookPath);
var bookPhasePositions = new List<Board>();

Console.WriteLine($"Opening book: {openingBook.Count} learned position(s) from {bookPath}");

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
        AnalyzeAndSaveBook();
        return 0;
    }

    StartPondering(board);

    Console.Write("Move (SAN/UCI), 'go [depth]', 'book teach <moves>', 'quit' to exit: ");
    string? input = Console.ReadLine();
    string trimmed = input?.Trim('﻿', ' ', '\t').Trim() ?? "quit";

    if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        ponderCts?.Cancel();
        AnalyzeAndSaveBook();
        return 0;
    }

    if (trimmed.StartsWith("book teach ", StringComparison.OrdinalIgnoreCase))
    {
        TeachRepertoireLine(trimmed[11..]);
        continue;
    }

    if (trimmed.Equals("go", StringComparison.OrdinalIgnoreCase) ||
        trimmed.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
    {
        int depth = SelectSearchDepth(board);
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && int.TryParse(parts[1], out int requestedDepth) && requestedDepth > 0)
            depth = requestedDepth;

        Move? engineMove;
        string tag;

        if (bookPhasePositions.Count < bookMaxPlies && openingBook.TryGetMove(board, out Move bookMove))
        {
            ponderCts?.Cancel();
            engineMove = bookMove;
            tag = "book";
        }
        else if (ponderTask is not null && ponderFen == board.ToFen() && ponderDepth == depth)
        {
            engineMove = ponderTask.GetAwaiter().GetResult();
            tag = "pondered";
        }
        else
        {
            ponderCts?.Cancel();
            engineMove = searcher.FindBestMove(board, depth);
            tag = "searched";
        }

        if (engineMove is null)
        {
            Console.WriteLine("Engine has no legal move.");
            continue;
        }

        string engineSan = moveFormatter.Format(board, engineMove.Value);
        Console.WriteLine($"Engine plays: {engineSan} (depth {depth}, {tag})");
        moveHistory.Add(engineSan);
        if (bookPhasePositions.Count < bookMaxPlies) bookPhasePositions.Add(board.Clone());
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
    if (bookPhasePositions.Count < bookMaxPlies) bookPhasePositions.Add(board.Clone());
    board.ApplyMove(parsedMove.Value);
}

void StartPondering(Board currentBoard)
{
    ponderCts?.Cancel();

    ponderCts = new CancellationTokenSource();
    CancellationToken token = ponderCts.Token;
    Board snapshot = currentBoard.Clone();

    ponderFen = currentBoard.ToFen();
    ponderDepth = SelectSearchDepth(currentBoard);

    ponderTask = Task.Run(() =>
    {
        try
        {
            return searcher.FindBestMove(snapshot, ponderDepth, token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }, token);
}

void AnalyzeAndSaveBook()
{
    if (bookPhasePositions.Count == 0) return;

    Console.WriteLine($"Analyzing {bookPhasePositions.Count} opening position(s) at depth {bookAnalysisDepth} to update the book...");

    for (int i = 0; i < bookPhasePositions.Count; i++)
    {
        Board position = bookPhasePositions[i];

        if (openingBook.IsLocked(position))
        {
            Console.WriteLine($"  [{i + 1}/{bookPhasePositions.Count}] {position.ToFen()} -> kept (locked repertoire move)");
            continue;
        }

        Move? best = searcher.FindBestMove(position, bookAnalysisDepth);
        if (best is null) continue;

        string san = moveFormatter.Format(position, best.Value);
        openingBook.Record(position, best.Value);
        Console.WriteLine($"  [{i + 1}/{bookPhasePositions.Count}] {position.ToFen()} -> {san}");
    }

    openingBook.Save(bookPath);
    Console.WriteLine($"Opening book saved: {openingBook.Count} position(s) -> {bookPath}");
}

// Feeds a space-separated SAN/UCI move list from the starting position (independent of the
// live game board) and locks each resulting position's move as a deliberately chosen
// repertoire entry, so AnalyzeAndSaveBook's post-game re-analysis will never overwrite it.
//
// An optional leading "--as white|black" restricts locking to positions where that color is
// to move. This matters for a repertoire line that exists to prepare a reply to an opponent's
// choice (e.g. "how Black meets 1.e4"): the opponent's own moves in that illustrative line
// aren't something we're prescribing, and different reply lines can share the very same
// starting position - locking every ply indiscriminately lets an unrelated line's opponent
// move silently overwrite our own actual repertoire choice there.
void TeachRepertoireLine(string args)
{
    string[] allTokens = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (allTokens.Length == 0)
    {
        Console.WriteLine("Usage: book teach [--as white|black] <move1> <move2> ...");
        return;
    }

    Color? recordOnlyFor = null;
    int start = 0;

    if (allTokens[0].Equals("--as", StringComparison.OrdinalIgnoreCase))
    {
        if (allTokens.Length < 2 ||
            !TryParseColor(allTokens[1], out Color parsedColor))
        {
            Console.WriteLine("Usage: book teach --as white|black <move1> <move2> ...");
            return;
        }

        recordOnlyFor = parsedColor;
        start = 2;
    }

    string[] tokens = allTokens[start..];
    if (tokens.Length == 0)
    {
        Console.WriteLine("Usage: book teach [--as white|black] <move1> <move2> ...");
        return;
    }

    Board teachBoard = Board.CreateStartingPosition();
    int processed = 0;
    int locked = 0;

    foreach (string token in tokens)
    {
        Move? teachMove = null;
        foreach (IMoveParser parser in moveParsers)
        {
            if (parser.TryParse(teachBoard, token, out Move candidate))
            {
                teachMove = candidate;
                break;
            }
        }

        if (teachMove is null)
        {
            Console.WriteLine($"Teach stopped: '{token}' is not legal after {processed} move(s).");
            break;
        }

        if (recordOnlyFor is null || teachBoard.SideToMove == recordOnlyFor)
        {
            openingBook.Record(teachBoard, teachMove.Value, locked: true);
            locked++;
        }

        teachBoard.ApplyMove(teachMove.Value);
        processed++;
    }

    if (locked == 0) return;

    openingBook.Save(bookPath);
    string scope = recordOnlyFor?.ToString() ?? "both sides";
    Console.WriteLine($"Taught and locked {locked} position(s) as {scope} (from {processed} move(s) played). Opening book: {openingBook.Count} total -> {bookPath}");
}

static bool TryParseColor(string text, out Color color)
{
    if (text.Equals("white", StringComparison.OrdinalIgnoreCase)) { color = Color.White; return true; }
    if (text.Equals("black", StringComparison.OrdinalIgnoreCase)) { color = Color.Black; return true; }
    color = default;
    return false;
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
