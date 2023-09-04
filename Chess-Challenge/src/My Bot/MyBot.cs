using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int totalMovesSearched = 0;
    int movesMade = 0;

    public Move Think(Board board, Timer timer)
    {
        var evaluation = Enumerable.Range(1, 100)
            .Aggregate((
                    move: Move.NullMove,
                    score: 0,
                    depth: 0
                ),
                (lastDepthMoveScore, depth) => 
                    timer.MillisecondsElapsedThisTurn > 1000
                    ? lastDepthMoveScore
                    : board.GetLegalMoves()
                        .Select(move => (
                            move: move, 
                            // score: board.MakeMove(move, board => -AlphaBeta(board, depth, () => false)),
                            score: board.MakeMove(move, board => -AlphaBeta(board, depth, () => timer.MillisecondsElapsedThisTurn > 1000)),
                            depth: depth
                        ))
                        .MaxBy(moveScore => moveScore.score)
            );

        movesMade += 1;

        Console.WriteLine($"Moves: {board.PlyCount} \t Score: {evaluation.score} \t Average Moves: {totalMovesSearched / movesMade} \t Time Elapsed: {timer.MillisecondsElapsedThisTurn} \t Depth: {evaluation.depth}");
        
        return evaluation.move;
    }

    int AlphaBeta(Board board, int depth, Func<bool> shouldCancel, int alpha = int.MinValue + 1, int beta = int.MaxValue) =>
        board.GetLegalMoves().Length == 0
            ? board.IsInCheckmate()
                ? -1000000
                : 0
            // : shouldCancel() || depth == 0
            : depth == 0
                ? Evaluate(board)
                : board.GetLegalMoves(depth <= 0 || shouldCancel())
                    .OrderByDescending(move =>
                        - Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                        + (move.CapturePieceType - move.MovePieceType)
                        + move.PromotionPieceType
                    )
                    .Aggregate(alpha, (alpha, move) =>
                        alpha >= beta
                            ? beta
                            : Math.Max(
                                alpha,
                                board.MakeMove(move, board => -AlphaBeta(board, depth - 1, shouldCancel, -beta, -alpha))
                            )
                    );

    int Evaluate(Board board)
    {
        totalMovesSearched += 1;

        return (
            board.GetAllPieceLists()
                .Sum(pieceList =>
                    new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                    * pieceList.Count
                    * (pieceList.IsWhitePieceList ? 1 : -1)
                )
            + board.GetAllPieceLists()
                .Sum(pieceList => pieceList.Sum(
                    piece => new Func<int, int, int>[] {
                        (x, y) => 0,
                        (x, y) => y * 8,
                        (x, y) => 20 - (int)(Math.Pow(x * 2 - 7, 4) + Math.Pow(y * 2 - 7, 4)) / 70,
                        (x, y) => 10 - (int)(Math.Pow(x * 2 - 7, 4) + Math.Pow(y * 2 - 7, 4)) / 100,
                        (x, y) => 0,
                        (x, y) => 5 - (int)(Math.Pow(x * 2 - 7, 4) + Math.Pow(y * 2 - 7, 4)) / 200,
                        (x, y) => (int)Math.Pow(x * 2 - 7, 2) / 2 + (50 - y * 8) - 50
                    } [(int)piece.PieceType]((piece.IsWhite ? 1 : -1) * piece.Square.Rank + (piece.IsWhite ? 1 : 7), (piece.IsWhite ? 1 : -1) * piece.Square.File + (piece.IsWhite ? 1 : 7))
                ))
            ) * (board.IsWhiteToMove ? 1 : -1);
    }
}

public static class Extensions
{
    public static T MakeMove<T>(this Board board, Move move, Func<Board, T> func)
    {
        board.MakeMove(move);
        T value = func(board);
        board.UndoMove(move);
        return value;
    }
}