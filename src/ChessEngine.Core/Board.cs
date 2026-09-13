using System;
using System.Globalization;
using System.Text;
using ChessEngine.Core.Moves;

namespace ChessEngine.Core;

/// <summary>
/// Represents the state of a chess board: piece placement plus the FEN side-state fields
/// (side to move, castling rights, en passant target, and the move clocks).
/// </summary>
public class Board
{
    public const int BoardSize = 8;
    public const string StartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Indexed [rank, file]: rank 0 = rank 1 (White's back rank), file 0 = file 'a'.
    private readonly Piece[,] _squares = new Piece[BoardSize, BoardSize];

    public Color SideToMove { get; set; } = Color.White;
    public CastlingRights Castling { get; set; } = CastlingRights.None;
    public Square? EnPassantTarget { get; set; }
    public int HalfmoveClock { get; set; }
    public int FullmoveNumber { get; set; } = 1;

    public Board()
    {
        for (int rank = 0; rank < BoardSize; rank++)
            for (int file = 0; file < BoardSize; file++)
                _squares[rank, file] = Piece.None;
    }

    public Piece GetPiece(Square square) => _squares[square.Rank, square.File];

    public Piece GetPiece(int file, int rank) => _squares[rank, file];

    public void SetPiece(Square square, Piece piece) => _squares[square.Rank, square.File] = piece;

    public void SetPiece(int file, int rank, Piece piece) => _squares[rank, file] = piece;

    /// <summary>Finds the square of the given color's king. Throws if the board has none.</summary>
    public Square FindKing(Color color)
    {
        for (int rank = 0; rank < BoardSize; rank++)
        {
            for (int file = 0; file < BoardSize; file++)
            {
                Piece piece = GetPiece(file, rank);
                if (piece.Type == PieceType.King && piece.Color == color)
                    return new Square(file, rank);
            }
        }

        throw new InvalidOperationException($"No {color} king found on the board.");
    }

    /// <summary>Creates an independent deep copy of this board.</summary>
    public Board Clone()
    {
        var clone = new Board
        {
            SideToMove = SideToMove,
            Castling = Castling,
            EnPassantTarget = EnPassantTarget,
            HalfmoveClock = HalfmoveClock,
            FullmoveNumber = FullmoveNumber
        };

        for (int rank = 0; rank < BoardSize; rank++)
            for (int file = 0; file < BoardSize; file++)
                clone.SetPiece(file, rank, GetPiece(file, rank));

        return clone;
    }

    /// <summary>
    /// Mutates this board by playing a pseudo-legal move: moves the piece, resolves captures
    /// (including en passant), moves the rook for castling, applies promotions, and updates
    /// castling rights, the en passant target, the move clocks, and the side to move.
    /// This does not verify legality (e.g. that the mover isn't left in check) - see MoveGenerator.
    /// </summary>
    public void ApplyMove(Move move)
    {
        Piece moving = GetPiece(move.From);
        bool isPawnMove = moving.Type == PieceType.Pawn;
        bool isCapture = move.IsCapture;

        if (move.IsEnPassant)
        {
            var capturedPawnSquare = new Square(move.To.File, move.From.Rank);
            SetPiece(capturedPawnSquare, Piece.None);
        }

        SetPiece(move.From, Piece.None);
        SetPiece(move.To, move.IsPromotion ? new Piece(move.Promotion, moving.Color) : moving);

        if (move.IsCastling)
        {
            int rank = move.From.Rank;
            bool isKingside = move.To.File > move.From.File;
            var rookFrom = new Square(isKingside ? 7 : 0, rank);
            var rookTo = new Square(isKingside ? 5 : 3, rank);
            SetPiece(rookTo, GetPiece(rookFrom));
            SetPiece(rookFrom, Piece.None);
        }

        UpdateCastlingRights(move, moving);

        EnPassantTarget = move.IsDoublePawnPush
            ? new Square(move.From.File, (move.From.Rank + move.To.Rank) / 2)
            : null;

        HalfmoveClock = isPawnMove || isCapture ? 0 : HalfmoveClock + 1;
        if (SideToMove == Color.Black) FullmoveNumber++;
        SideToMove = SideToMove.Opposite();
    }

    private void UpdateCastlingRights(Move move, Piece moving)
    {
        if (moving.Type == PieceType.King)
        {
            Castling &= moving.Color == Color.White
                ? ~(CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside)
                : ~(CastlingRights.BlackKingside | CastlingRights.BlackQueenside);
        }

        ClearCastlingRightIfRookSquare(move.From);
        ClearCastlingRightIfRookSquare(move.To);
    }

    private void ClearCastlingRightIfRookSquare(Square square)
    {
        Castling &= (square.File, square.Rank) switch
        {
            (0, 0) => ~CastlingRights.WhiteQueenside,
            (7, 0) => ~CastlingRights.WhiteKingside,
            (0, 7) => ~CastlingRights.BlackQueenside,
            (7, 7) => ~CastlingRights.BlackKingside,
            _ => ~CastlingRights.None
        };
    }

    public static Board CreateStartingPosition() => FromFen(StartingFen);

    /// <summary>Parses a full FEN record into a new Board.</summary>
    public static Board FromFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
            throw new ArgumentException("FEN string must not be empty.", nameof(fen));

        string[] fields = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 1)
            throw new FormatException("FEN string is missing the piece placement field.");

        var board = new Board();
        ParsePiecePlacement(board, fields[0]);

        board.SideToMove = fields.Length > 1 && fields[1] == "b" ? Color.Black : Color.White;
        board.Castling = fields.Length > 2 ? ParseCastling(fields[2]) : CastlingRights.None;
        board.EnPassantTarget = fields.Length > 3 && fields[3] != "-" ? Square.FromAlgebraic(fields[3]) : null;
        board.HalfmoveClock = fields.Length > 4 ? int.Parse(fields[4], CultureInfo.InvariantCulture) : 0;
        board.FullmoveNumber = fields.Length > 5 ? int.Parse(fields[5], CultureInfo.InvariantCulture) : 1;

        return board;
    }

    private static void ParsePiecePlacement(Board board, string placement)
    {
        string[] rankRows = placement.Split('/');
        if (rankRows.Length != BoardSize)
            throw new FormatException($"FEN piece placement must have {BoardSize} ranks separated by '/'.");

        for (int i = 0; i < BoardSize; i++)
        {
            int rank = BoardSize - 1 - i; // rankRows[0] is rank 8
            int file = 0;

            foreach (char c in rankRows[i])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    if (file >= BoardSize)
                        throw new FormatException($"FEN rank '{rankRows[i]}' overflows the board width.");

                    board.SetPiece(file, rank, Piece.FromFenChar(c));
                    file++;
                }
            }

            if (file != BoardSize)
                throw new FormatException($"FEN rank '{rankRows[i]}' does not describe exactly {BoardSize} files.");
        }
    }

    private static CastlingRights ParseCastling(string field)
    {
        if (field == "-") return CastlingRights.None;

        var rights = CastlingRights.None;
        foreach (char c in field)
        {
            rights |= c switch
            {
                'K' => CastlingRights.WhiteKingside,
                'Q' => CastlingRights.WhiteQueenside,
                'k' => CastlingRights.BlackKingside,
                'q' => CastlingRights.BlackQueenside,
                _ => throw new FormatException($"'{c}' is not a valid castling availability character.")
            };
        }

        return rights;
    }

    /// <summary>Serializes this board back into a full FEN record.</summary>
    public string ToFen()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < BoardSize; i++)
        {
            int rank = BoardSize - 1 - i;
            int emptyRun = 0;

            for (int file = 0; file < BoardSize; file++)
            {
                Piece piece = GetPiece(file, rank);
                if (piece.IsNone)
                {
                    emptyRun++;
                    continue;
                }

                if (emptyRun > 0)
                {
                    sb.Append(emptyRun);
                    emptyRun = 0;
                }

                sb.Append(piece.ToFenChar());
            }

            if (emptyRun > 0) sb.Append(emptyRun);
            if (i < BoardSize - 1) sb.Append('/');
        }

        sb.Append(' ').Append(SideToMove == Color.White ? 'w' : 'b');
        sb.Append(' ').Append(FormatCastling(Castling));
        sb.Append(' ').Append(EnPassantTarget?.ToAlgebraic() ?? "-");
        sb.Append(' ').Append(HalfmoveClock.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ').Append(FullmoveNumber.ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    private static string FormatCastling(CastlingRights rights)
    {
        if (rights == CastlingRights.None) return "-";

        var sb = new StringBuilder();
        if (rights.HasFlag(CastlingRights.WhiteKingside)) sb.Append('K');
        if (rights.HasFlag(CastlingRights.WhiteQueenside)) sb.Append('Q');
        if (rights.HasFlag(CastlingRights.BlackKingside)) sb.Append('k');
        if (rights.HasFlag(CastlingRights.BlackQueenside)) sb.Append('q');
        return sb.ToString();
    }

    public override string ToString() => ToFen();
}
