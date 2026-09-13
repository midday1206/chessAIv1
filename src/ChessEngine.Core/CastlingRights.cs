using System;

namespace ChessEngine.Core;

[Flags]
public enum CastlingRights
{
    None = 0,
    WhiteKingside = 1 << 0,
    WhiteQueenside = 1 << 1,
    BlackKingside = 1 << 2,
    BlackQueenside = 1 << 3,
    All = WhiteKingside | WhiteQueenside | BlackKingside | BlackQueenside
}
