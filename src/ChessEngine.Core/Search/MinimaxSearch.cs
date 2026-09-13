using System;
using System.Collections.Generic;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Search;

/// <summary>
/// Fixed-depth minimax search with alpha-beta pruning, implemented as negamax (the position
/// is always scored from the side-to-move's perspective, negating as the recursion unwinds).
/// Checkmate/stalemate are treated as exact terminal scores rather than left to the evaluator.
/// </summary>
public sealed class MinimaxSearch : IMoveSearcher
{
    private const int MateScore = 1_000_000;
    private const int Infinity = int.MaxValue;

    private readonly IPositionEvaluator _evaluator;

    public MinimaxSearch(IPositionEvaluator evaluator) => _evaluator = evaluator;

    public Move? FindBestMove(Board board, int depth)
    {
        if (depth < 1)
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Search depth must be at least 1 ply.");

        List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(board);
        if (legalMoves.Count == 0) return null;

        Move? bestMove = null;
        int bestScore = -Infinity;
        int alpha = -Infinity;
        const int beta = Infinity;

        foreach (Move move in legalMoves)
        {
            Board next = board.Clone();
            next.ApplyMove(move);
            int score = -Negamax(next, depth - 1, -beta, -alpha);

            if (bestMove is null || score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, score);
        }

        return bestMove;
    }

    private int Negamax(Board board, int depthRemaining, int alpha, int beta)
    {
        GameStatus status = MoveGenerator.GetGameStatus(board);
        if (status == GameStatus.Checkmate) return -(MateScore + depthRemaining);
        if (status == GameStatus.Stalemate) return 0;
        if (depthRemaining == 0) return Perspective(board) * _evaluator.Evaluate(board);

        int best = -Infinity;
        foreach (Move move in MoveGenerator.GenerateLegalMoves(board))
        {
            Board next = board.Clone();
            next.ApplyMove(move);
            int score = -Negamax(next, depthRemaining - 1, -beta, -alpha);

            if (score > best) best = score;
            if (best > alpha) alpha = best;
            if (alpha >= beta) break; // beta cutoff: the opponent won't allow this line
        }

        return best;
    }

    private static int Perspective(Board board) => board.SideToMove == Color.White ? 1 : -1;
}
