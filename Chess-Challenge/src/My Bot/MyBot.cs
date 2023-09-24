using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;
using Microsoft.CodeAnalysis;

public class MyBot : IChessBot
{
    Func<int, int> factorial;
    public int TestFunction() =>
        (factorial = new(x => x == 0 ? 1 : x * factorial(x - 1)))(10);

    Func<Board, int, int, int, Func<bool>, int> search;
    public Move Think(Board board, Timer timer)
    {
        var evaluation = Enumerable.Range(0, 100)
            .Aggregate(
                (
                    nightly: (depth: 0, score: int.MinValue + 1, move: Move.NullMove),
                    stable: (depth: -1, score: int.MinValue + 1, move: Move.NullMove)
                ),
                (previousEvaluation, currentDepth) =>
                    timer.MillisecondsElapsedThisTurn > 1000                    // \
                        ? previousEvaluation                                    //  | Don't start a new search if time is up
                        : Enumerable.Repeat(                                    // /
                                board.GetLegalMoves()
                                    .Aggregate(
                                        (
                                            bestMove: Move.NullMove,
                                            scoreOfBestMove: int.MinValue + 1
                                        ),
                                        (previousCheck, move) => 
                                            Enumerable.Repeat(
                                                board.MakeMove(move, board => 
                                                    (search = new(
                                                        (board, depth, alpha, beta, shouldCancel) => 
                                                            board.GetLegalMoves().Length == 0
                                                                ? board.IsInCheckmate() ? -1000000 : 0
                                                                : Enumerable.Repeat(
                                                                    depth <= 0
                                                                        ? (board.GetAllPieceLists()
                                                                            .Sum(pieceList =>
                                                                                new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                * pieceList.Count
                                                                                * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                            )
                                                                        + board.GetAllPieceLists()
                                                                            .Sum(pieceList => pieceList.Sum(
                                                                                piece => new Func<int, int, int>[] {
                                                                                    (x, y) => 0,
                                                                                    (x, y) => 3 * (y - 1) * (8 - Math.Abs((x * 2) - 7)),
                                                                                    (x, y) => 20 - (int)(Math.Pow(x * 2 - 7, 2) + Math.Pow(y * 2 - 7, 2)),
                                                                                    (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                                    (x, y) => 0,
                                                                                    (x, y) => 0,
                                                                                    (x, y) => (Math.Abs(x * 2 - 7) / 2 - y * 2) * 8
                                                                                }[(int)piece.PieceType](piece.IsWhite ? piece.Square.File : 7 - piece.Square.File, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank)
                                                                            ) * (pieceList.IsWhitePieceList ? 1 : -1))
                                                                        ) * (board.IsWhiteToMove ? 1 : -1)
                                                                        : int.MinValue,
                                                                    1
                                                                )
                                                                    .Select(
                                                                        standPat => shouldCancel()
                                                                            ? standPat
                                                                            : board.GetLegalMoves(depth <= 0)
                                                                                .OrderByDescending(
                                                                                    move =>
                                                                                        - Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                                                                                        + (move.CapturePieceType - move.MovePieceType)
                                                                                        + (int)move.PromotionPieceType
                                                                                )
                                                                                .Aggregate(
                                                                                    Math.Max(alpha, standPat),
                                                                                    (alpha, move) =>
                                                                                        alpha >= beta
                                                                                            ? beta
                                                                                            : Math.Max(
                                                                                                alpha,
                                                                                                board.MakeMove(move, board => -search(board, depth - 1, -beta, -alpha, shouldCancel))
                                                                                            )

                                                                                )
                                                                    )
                                                                    .First()
                                                                
                                                                // depth == 0 || shouldCancel()
                                                                //     ? (board.GetAllPieceLists()
                                                                //         .Sum(pieceList =>
                                                                //             new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                //             * pieceList.Count
                                                                //             * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                //         )
                                                                //     + board.GetAllPieceLists()
                                                                //         .Sum(pieceList => pieceList.Sum(
                                                                //             piece => new Func<int, int, int>[] {
                                                                //                 (x, y) => 0,
                                                                //                 (x, y) => 3 * (y - 1) * (8 - Math.Abs((x * 2) - 7)),
                                                                //                 (x, y) => 20 - (int)(Math.Pow(x * 2 - 7, 2) + Math.Pow(y * 2 - 7, 2)),
                                                                //                 (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                //                 (x, y) => 0,
                                                                //                 (x, y) => 0,
                                                                //                 (x, y) => (Math.Abs(x * 2 - 7) / 2 - y * 2) * 8
                                                                //             }[(int)piece.PieceType](piece.IsWhite ? piece.Square.File : 7 - piece.Square.File, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank)
                                                                //         ))
                                                                //     ) * (board.IsWhiteToMove ? 1 : -1)

                                                                //     : board.GetLegalMoves()
                                                                //         .OrderByDescending(move =>
                                                                //             - Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                                                                //             + (move.CapturePieceType - move.MovePieceType)
                                                                //             + (int)move.PromotionPieceType
                                                                //         )
                                                                //         .Aggregate(
                                                                //             alpha,
                                                                //             (alpha, move) =>
                                                                //                 alpha >= beta
                                                                //                     ? beta
                                                                //                     : Math.Max(
                                                                //                         alpha,
                                                                //                         board.MakeMove(move, board => -search(board, depth - 1, -beta, -alpha, shouldCancel))
                                                                //                     )

                                                                //         )
                                                    ))(board, currentDepth, int.MinValue + 1, int.MaxValue, () => timer.MillisecondsElapsedThisTurn > 1000)
                                                ),
                                                1
                                            )
                                                .Select(
                                                    score => 
                                                        new[] {
                                                            (previousCheck.bestMove, previousCheck.scoreOfBestMove),
                                                            (move, -score)
                                                        }
                                                            .MaxBy(move_score => move_score.Item2)
                                                )
                                                .First()
                                    ),
                                1
                            )
                                .Select(
                                    move_score => (
                                        nightly: (
                                            depth: currentDepth,
                                            score: move_score.Item2,
                                            move: move_score.Item1
                                        ),
                                        stable: previousEvaluation.nightly
                                    )
                                )
                                .First()
            );

        Console.WriteLine($"Moves: {board.PlyCount} \t Score: {evaluation.stable.score} \t Time Elapsed: {timer.MillisecondsElapsedThisTurn} \t Depth: {evaluation.stable.depth}");
        
        return evaluation.stable.move;
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