# ChessEngine

A C# chess engine, built as a .NET solution with three projects:

- **ChessEngine.Core** - board/piece model and core chess logic.
- **ChessEngine.Cli** - console front-end for interacting with the engine.
- **ChessEngine.Tests** - NUnit test suite for Core.

## Status

Currently implements the board representation:

- `Piece` / `PieceType` / `Color` - piece identity, with FEN character conversion.
- `Square` - algebraic-notation coordinates (e.g. `e4`).
- `CastlingRights` - castling availability flags.
- `Board` - 8x8 board state, parseable from and serializable to a full FEN record.

## Usage

```bash
dotnet run --project src/ChessEngine.Cli
```

Optionally pass a FEN string to render a specific position:

```bash
dotnet run --project src/ChessEngine.Cli -- "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1"
```

## Testing

```bash
dotnet test
```
