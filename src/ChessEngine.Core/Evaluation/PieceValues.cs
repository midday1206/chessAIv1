using System.Collections.Generic;

namespace ChessEngine.Core.Evaluation;

/// <summary>Standard material point values, shared by every evaluator that needs them.</summary>
internal static class PieceValues
{
    public static readonly IReadOnlyDictionary<PieceType, int> Values = new Dictionary<PieceType, int>
    {
        [PieceType.Pawn] = 100,
        [PieceType.Knight] = 320,
        [PieceType.Bishop] = 330,
        [PieceType.Rook] = 500,
        [PieceType.Queen] = 900,
        [PieceType.King] = 0
    };
}
