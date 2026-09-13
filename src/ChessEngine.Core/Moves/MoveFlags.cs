using System;

namespace ChessEngine.Core.Moves;

[Flags]
public enum MoveFlags
{
    None = 0,
    Capture = 1 << 0,
    EnPassant = 1 << 1,
    Castling = 1 << 2,
    DoublePawnPush = 1 << 3
}
