using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core.Search;

/// <summary>
/// Fixed-depth minimax search with alpha-beta pruning, implemented as negamax (the position
/// is always scored from the side-to-move's perspective, negating as the recursion unwinds).
/// Checkmate/stalemate are treated as exact terminal scores rather than left to the evaluator.
/// Leaf nodes are resolved with a quiescence search (see Quiescence) instead of evaluated
/// statically, so the search doesn't stop mid-exchange and misjudge who actually wins material.
/// Moves are tried captures-first (MVV-LVA) so alpha-beta finds strong refutations sooner -
/// without it, quiescence's extra nodes make deeper searches too slow to be interactive.
/// </summary>
public sealed class MinimaxSearch : IMoveSearcher
{
    private const int MateScore = 1_000_000;
    private const int Infinity = int.MaxValue;
    private const int MaxQuiescenceDepth = 4;

    // At the root, a move scoring within this many centipawns of the best found is treated
    // as an effective tie. Among ties, a non-capturing move is preferred over a capture -
    // if trading down doesn't actually score any better, don't simplify the position, since
    // this engine's edge is calculation and calculation matters more with more on the board.
    private const int TradeAvoidanceMargin = 20;

    private readonly IPositionEvaluator _evaluator;

    public MinimaxSearch(IPositionEvaluator evaluator) => _evaluator = evaluator;

    public Move? FindBestMove(Board board, int depth, CancellationToken cancellationToken = default)
    {
        if (depth < 1)
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Search depth must be at least 1 ply.");

        List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(board);
        if (legalMoves.Count == 0) return null;

        Move? bestMove = null;
        int bestScore = -Infinity;
        Move? bestQuietMove = null;
        int bestQuietScore = -Infinity;
        int alpha = -Infinity;
        const int beta = Infinity;

        foreach (Move move in OrderMoves(board, legalMoves))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Board next = board.Clone();
            next.ApplyMove(move);
            int score = -Negamax(next, depth - 1, -beta, -alpha, cancellationToken);

            if (bestMove is null || score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            if (!move.IsCapture && (bestQuietMove is null || score > bestQuietScore))
            {
                bestQuietScore = score;
                bestQuietMove = move;
            }

            alpha = Math.Max(alpha, score);
        }

        // Trade-avoidance tie-break: the objectively best move (found above exactly as
        // plain alpha-beta would) might be a capture, while a quiet move scored close to it.
        // That quiet move's score above may only be a pruned bound rather than its true
        // value (tightening alpha across root siblings is what makes this search fast), so
        // resolve the comparison with one exact, fully-open-window re-search - just for this
        // single candidate, not per move, which is what made an earlier version of this
        // check pathologically slow in richer positions.
        if (bestMove is not null && bestMove.Value.IsCapture && bestQuietMove is not null &&
            bestQuietScore > bestScore - TradeAvoidanceMargin - 1)
        {
            Board quietNext = board.Clone();
            quietNext.ApplyMove(bestQuietMove.Value);
            int exactQuietScore = -Negamax(quietNext, depth - 1, -Infinity, Infinity, cancellationToken);

            if (exactQuietScore >= bestScore - TradeAvoidanceMargin)
                return bestQuietMove;
        }

        return bestMove;
    }

    private int Negamax(Board board, int depthRemaining, int alpha, int beta, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool inCheck = MoveGenerator.IsInCheck(board, board.SideToMove);
        List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(board);

        if (legalMoves.Count == 0) return inCheck ? -(MateScore + depthRemaining) : 0;
        if (depthRemaining == 0) return Quiescence(board, alpha, beta, 0, cancellationToken);

        int best = -Infinity;
        foreach (Move move in OrderMoves(board, legalMoves))
        {
            Board next = board.Clone();
            next.ApplyMove(move);
            int score = -Negamax(next, depthRemaining - 1, -beta, -alpha, cancellationToken);

            if (score > best) best = score;
            if (best > alpha) alpha = best;
            if (alpha >= beta) break; // beta cutoff: the opponent won't allow this line
        }

        return best;
    }

    /// <summary>
    /// Extends the search past the depth horizon along "noisy" lines only (captures and
    /// promotions, or every legal move while in check - a check can't just be ignored) until
    /// the position quiets down, so an in-progress exchange is never evaluated mid-trade.
    /// The side to move may always "stand pat" (decline to continue trading), which both
    /// bounds the recursion and lets a bad trade be refused.
    /// </summary>
    private int Quiescence(Board board, int alpha, int beta, int qDepth, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool inCheck = MoveGenerator.IsInCheck(board, board.SideToMove);
        List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(board);

        if (legalMoves.Count == 0) return inCheck ? -MateScore : 0;

        int standPat = Perspective(board) * _evaluator.Evaluate(board);

        if (!inCheck)
        {
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;
        }

        if (qDepth >= MaxQuiescenceDepth) return inCheck ? alpha : Math.Max(alpha, standPat);

        IEnumerable<Move> candidates = inCheck ? legalMoves : legalMoves.Where(m => m.IsCapture || m.IsPromotion);

        foreach (Move move in OrderMoves(board, candidates))
        {
            Board next = board.Clone();
            next.ApplyMove(move);
            int score = -Quiescence(next, -beta, -alpha, qDepth + 1, cancellationToken);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }

    /// <summary>
    /// Orders moves captures-first (biggest expected material swing first, MVV-LVA), with
    /// promotions boosted too. This is what makes alpha-beta pruning actually effective -
    /// a good move tried early prunes far more of the tree than one tried late.
    /// </summary>
    private static IEnumerable<Move> OrderMoves(Board board, IEnumerable<Move> moves) =>
        moves.OrderByDescending(m => MoveOrderingScore(board, m));

    private static int MoveOrderingScore(Board board, Move move)
    {
        int score = 0;

        if (move.IsCapture)
        {
            PieceType victim = move.IsEnPassant ? PieceType.Pawn : board.GetPiece(move.To).Type;
            PieceType attacker = board.GetPiece(move.From).Type;
            // Most Valuable Victim, Least Valuable Attacker: prefer big captures with cheap pieces.
            score += (PieceValues.Values[victim] * 10) - PieceValues.Values[attacker];
        }

        if (move.IsPromotion) score += PieceValues.Values[move.Promotion];

        return score;
    }

    private static int Perspective(Board board) => board.SideToMove == Color.White ? 1 : -1;
}
