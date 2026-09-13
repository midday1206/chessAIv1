using System.Collections.Generic;

namespace ChessEngine.Core.Moves;

/// <summary>Generates pseudo-legal and legal moves for a Board, and answers square-attack queries.</summary>
public static class MoveGenerator
{
    private static readonly (int DFile, int DRank)[] KnightOffsets =
    {
        (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)
    };

    private static readonly (int DFile, int DRank)[] BishopDirections =
    {
        (1, 1), (1, -1), (-1, 1), (-1, -1)
    };

    private static readonly (int DFile, int DRank)[] RookDirections =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1)
    };

    private static readonly (int DFile, int DRank)[] KingOffsets =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)
    };

    /// <summary>
    /// Generates every pseudo-legal move for the side to move: obeys each piece's movement
    /// rules but does not check whether the mover's own king ends up in check.
    /// </summary>
    public static List<Move> GeneratePseudoLegalMoves(Board board)
    {
        var moves = new List<Move>();
        Color side = board.SideToMove;

        for (int rank = 0; rank < Board.BoardSize; rank++)
        {
            for (int file = 0; file < Board.BoardSize; file++)
            {
                Piece piece = board.GetPiece(file, rank);
                if (piece.IsNone || piece.Color != side) continue;

                var from = new Square(file, rank);
                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        GeneratePawnMoves(board, from, side, moves);
                        break;
                    case PieceType.Knight:
                        GenerateOffsetMoves(board, from, side, KnightOffsets, moves);
                        break;
                    case PieceType.Bishop:
                        GenerateSlidingMoves(board, from, side, BishopDirections, moves);
                        break;
                    case PieceType.Rook:
                        GenerateSlidingMoves(board, from, side, RookDirections, moves);
                        break;
                    case PieceType.Queen:
                        GenerateSlidingMoves(board, from, side, BishopDirections, moves);
                        GenerateSlidingMoves(board, from, side, RookDirections, moves);
                        break;
                    case PieceType.King:
                        GenerateOffsetMoves(board, from, side, KingOffsets, moves);
                        GenerateCastlingMoves(board, side, moves);
                        break;
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// Generates only the legal moves for the side to move: pseudo-legal moves that do not
    /// leave the mover's own king in check.
    /// </summary>
    public static List<Move> GenerateLegalMoves(Board board)
    {
        Color side = board.SideToMove;
        var legalMoves = new List<Move>();

        foreach (Move move in GeneratePseudoLegalMoves(board))
        {
            Board resulting = board.Clone();
            resulting.ApplyMove(move);

            if (!IsSquareAttacked(resulting, resulting.FindKing(side), side.Opposite()))
                legalMoves.Add(move);
        }

        return legalMoves;
    }

    /// <summary>Whether the given color's king is currently attacked.</summary>
    public static bool IsInCheck(Board board, Color color) =>
        IsSquareAttacked(board, board.FindKing(color), color.Opposite());

    /// <summary>
    /// Classifies the position for the side to move: Checkmate/Stalemate when it has no legal
    /// moves (depending on whether it's in check), Check when it has legal moves but is in
    /// check, otherwise InProgress.
    /// </summary>
    public static GameStatus GetGameStatus(Board board)
    {
        bool inCheck = IsInCheck(board, board.SideToMove);
        bool hasLegalMoves = GenerateLegalMoves(board).Count > 0;

        if (hasLegalMoves) return inCheck ? GameStatus.Check : GameStatus.InProgress;
        return inCheck ? GameStatus.Checkmate : GameStatus.Stalemate;
    }

    /// <summary>Whether the given square is attacked by any piece of the given color.</summary>
    public static bool IsSquareAttacked(Board board, Square square, Color byColor)
    {
        if (IsAttackedByPawn(board, square, byColor)) return true;
        if (IsAttackedByOffsetPiece(board, square, byColor, KnightOffsets, PieceType.Knight)) return true;
        if (IsAttackedByOffsetPiece(board, square, byColor, KingOffsets, PieceType.King)) return true;
        if (IsAttackedBySlidingPiece(board, square, byColor, BishopDirections, PieceType.Bishop)) return true;
        if (IsAttackedBySlidingPiece(board, square, byColor, RookDirections, PieceType.Rook)) return true;
        return false;
    }

    private static bool IsAttackedByPawn(Board board, Square square, Color byColor)
    {
        int pawnRank = byColor == Color.White ? square.Rank - 1 : square.Rank + 1;
        if (pawnRank is < 0 or > 7) return false;

        foreach (int df in new[] { -1, 1 })
        {
            int file = square.File + df;
            if (file is < 0 or > 7) continue;

            Piece piece = board.GetPiece(file, pawnRank);
            if (piece.Type == PieceType.Pawn && piece.Color == byColor) return true;
        }

        return false;
    }

    private static bool IsAttackedByOffsetPiece(
        Board board, Square square, Color byColor, (int DFile, int DRank)[] offsets, PieceType type)
    {
        foreach (var (df, dr) in offsets)
        {
            int file = square.File + df;
            int rank = square.Rank + dr;
            if (file is < 0 or > 7 || rank is < 0 or > 7) continue;

            Piece piece = board.GetPiece(file, rank);
            if (piece.Type == type && piece.Color == byColor) return true;
        }

        return false;
    }

    private static bool IsAttackedBySlidingPiece(
        Board board, Square square, Color byColor, (int DFile, int DRank)[] directions, PieceType primaryType)
    {
        foreach (var (df, dr) in directions)
        {
            int file = square.File + df;
            int rank = square.Rank + dr;

            while (file is >= 0 and <= 7 && rank is >= 0 and <= 7)
            {
                Piece piece = board.GetPiece(file, rank);
                if (!piece.IsNone)
                {
                    if (piece.Color == byColor && (piece.Type == primaryType || piece.Type == PieceType.Queen))
                        return true;
                    break;
                }

                file += df;
                rank += dr;
            }
        }

        return false;
    }

    private static void GeneratePawnMoves(Board board, Square from, Color side, List<Move> moves)
    {
        int direction = side == Color.White ? 1 : -1;
        int startRank = side == Color.White ? 1 : 6;
        int promotionRank = side == Color.White ? 7 : 0;

        int oneRank = from.Rank + direction;
        if (oneRank is >= 0 and <= 7 && board.GetPiece(from.File, oneRank).IsNone)
        {
            AddPawnMove(from, new Square(from.File, oneRank), promotionRank, MoveFlags.None, moves);

            int twoRank = from.Rank + (2 * direction);
            if (from.Rank == startRank && board.GetPiece(from.File, twoRank).IsNone)
                moves.Add(new Move(from, new Square(from.File, twoRank), flags: MoveFlags.DoublePawnPush));
        }

        foreach (int df in new[] { -1, 1 })
        {
            int file = from.File + df;
            if (file is < 0 or > 7 || oneRank is < 0 or > 7) continue;

            var target = new Square(file, oneRank);
            Piece targetPiece = board.GetPiece(target);

            if (!targetPiece.IsNone && targetPiece.Color != side)
            {
                AddPawnMove(from, target, promotionRank, MoveFlags.Capture, moves);
            }
            else if (targetPiece.IsNone && board.EnPassantTarget == target)
            {
                moves.Add(new Move(from, target, flags: MoveFlags.Capture | MoveFlags.EnPassant));
            }
        }
    }

    private static void AddPawnMove(Square from, Square to, int promotionRank, MoveFlags flags, List<Move> moves)
    {
        if (to.Rank == promotionRank)
        {
            moves.Add(new Move(from, to, PieceType.Queen, flags));
            moves.Add(new Move(from, to, PieceType.Rook, flags));
            moves.Add(new Move(from, to, PieceType.Bishop, flags));
            moves.Add(new Move(from, to, PieceType.Knight, flags));
        }
        else
        {
            moves.Add(new Move(from, to, flags: flags));
        }
    }

    private static void GenerateOffsetMoves(
        Board board, Square from, Color side, (int DFile, int DRank)[] offsets, List<Move> moves)
    {
        foreach (var (df, dr) in offsets)
        {
            int file = from.File + df;
            int rank = from.Rank + dr;
            if (file is < 0 or > 7 || rank is < 0 or > 7) continue;

            var to = new Square(file, rank);
            Piece targetPiece = board.GetPiece(to);
            if (targetPiece.IsNone)
                moves.Add(new Move(from, to));
            else if (targetPiece.Color != side)
                moves.Add(new Move(from, to, flags: MoveFlags.Capture));
        }
    }

    private static void GenerateSlidingMoves(
        Board board, Square from, Color side, (int DFile, int DRank)[] directions, List<Move> moves)
    {
        foreach (var (df, dr) in directions)
        {
            int file = from.File + df;
            int rank = from.Rank + dr;

            while (file is >= 0 and <= 7 && rank is >= 0 and <= 7)
            {
                var to = new Square(file, rank);
                Piece targetPiece = board.GetPiece(to);

                if (targetPiece.IsNone)
                {
                    moves.Add(new Move(from, to));
                }
                else
                {
                    if (targetPiece.Color != side)
                        moves.Add(new Move(from, to, flags: MoveFlags.Capture));
                    break;
                }

                file += df;
                rank += dr;
            }
        }
    }

    private static void GenerateCastlingMoves(Board board, Color side, List<Move> moves)
    {
        int rank = side == Color.White ? 0 : 7;
        var kingFrom = new Square(4, rank);
        Color opponent = side.Opposite();

        if (IsSquareAttacked(board, kingFrom, opponent)) return;

        CastlingRights kingsideRight = side == Color.White ? CastlingRights.WhiteKingside : CastlingRights.BlackKingside;
        if (board.Castling.HasFlag(kingsideRight)
            && board.GetPiece(5, rank).IsNone && board.GetPiece(6, rank).IsNone
            && !IsSquareAttacked(board, new Square(5, rank), opponent)
            && !IsSquareAttacked(board, new Square(6, rank), opponent))
        {
            moves.Add(new Move(kingFrom, new Square(6, rank), flags: MoveFlags.Castling));
        }

        CastlingRights queensideRight = side == Color.White ? CastlingRights.WhiteQueenside : CastlingRights.BlackQueenside;
        if (board.Castling.HasFlag(queensideRight)
            && board.GetPiece(1, rank).IsNone && board.GetPiece(2, rank).IsNone && board.GetPiece(3, rank).IsNone
            && !IsSquareAttacked(board, new Square(3, rank), opponent)
            && !IsSquareAttacked(board, new Square(2, rank), opponent))
        {
            moves.Add(new Move(kingFrom, new Square(2, rank), flags: MoveFlags.Castling));
        }
    }
}
