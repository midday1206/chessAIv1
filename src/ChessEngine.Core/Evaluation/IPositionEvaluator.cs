namespace ChessEngine.Core.Evaluation;

/// <summary>
/// A port for scoring a position. MaterialEvaluator is the first implementation; a fancier
/// evaluator (piece-square tables, mobility, king safety, ...) can implement this same
/// interface later without changing the search code that consumes it.
/// </summary>
public interface IPositionEvaluator
{
    /// <summary>Scores the position from White's perspective: positive favors White, negative favors Black.</summary>
    int Evaluate(Board board);
}
