using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    Func<int, int> factorial;
    public int TestFunction() =>
        (factorial = new(x => x == 0 ? 1 : x * factorial(x - 1)))(10);

    Func<Board, int, (ulong, int)[], int, int, Func<bool>, (int, (ulong, int)[])> search;
    public Move Think(Board board, Timer timer)
    {
        var evaluation = Enumerable.Range(0, 100)
            .Aggregate(
                (
                    depth: 0,
                    score: int.MinValue + 1,
                    move: Move.NullMove,
                    transpositionTable: Array.Empty<(ulong, int)>(),
                    previousEvaluationMove: Move.NullMove
                ),
                (previousDepthResult, currentDepth) =>
                    timer.MillisecondsElapsedThisTurn > 1000                    // \
                        ? previousDepthResult                                    //  | Don't start a new search if time is up
                        : Enumerable.Repeat(                                    // /
                                board.GetLegalMoves()
                                    .Aggregate(
                                        (
                                            bestMove: Move.NullMove,
                                            scoreOfBestMove: int.MinValue + 1,
                                            transpositionTable: previousDepthResult.transpositionTable
                                        ),
                                        (previousCheck, move) => 
                                            Enumerable.Repeat(
                                                board.MakeMove(move, board => 
                                                    (search = new(
                                                        (board, depth, transpositionTable, alpha, beta, shouldCancel) => 
                                                            board.GetLegalMoves().Length == 0
                                                                ? (board.IsInCheckmate() ? -1000000 : 0, transpositionTable)        // We don't add to the transposition table because there's no point — this position will always be checkmate
                                                                : depth == 0 || shouldCancel()
                                                                    ? Enumerable.Repeat(
                                                                        (board.GetAllPieceLists()
                                                                            .Sum(pieceList =>
                                                                                new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                * pieceList.Count
                                                                                * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                            )
                                                                        // + board.GetAllPieceLists()
                                                                        //     .Sum(pieceList => pieceList.Sum(
                                                                        //         piece => new Func<int, int, int>[] {
                                                                        //             (x, y) => 0,
                                                                        //             (x, y) => y == 6 ? 50 : 0, // + Convert.ToInt32(x == 3 || x == 4) * new[] { 0, -20, 0, 20, 0, 0, 0, 0 }[y],
                                                                        //             (x, y) => 20 - (int)(Math.Pow(x * 2 - 7, 2) + Math.Pow(y * 2 - 7, 2)),
                                                                        //             (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                        //             (x, y) => 0,
                                                                        //             (x, y) => 0,
                                                                        //             (x, y) => (Math.Abs(x * 2 - 7) / 2 - y * 2) * 8
                                                                        //         } [(int)piece.PieceType]((piece.IsWhite ? 1 : -1) * piece.Square.Rank + (piece.IsWhite ? 1 : 7), (piece.IsWhite ? 1 : -1) * piece.Square.File + (piece.IsWhite ? 1 : 7))
                                                                        //     ))
                                                                        ) * (board.IsWhiteToMove ? 1 : -1),
                                                                        1
                                                                    )
                                                                        .Select(score => (
                                                                            score,
                                                                            transpositionTable.Append((board.ZobristKey, score)).ToArray()
                                                                        ))
                                                                        .First()

                                                                    : board.GetLegalMoves()
                                                                        .OrderByDescending(move =>
                                                                            + transpositionTable
                                                                                .Where(transposition => transposition.Item1 == board.ZobristKey)
                                                                                .Sum(keyScore => keyScore.Item2)
                                                                            - 10 * Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                                                                            + 10 * (move.CapturePieceType - move.MovePieceType)
                                                                            + 10 * (int)move.PromotionPieceType
                                                                        )
                                                                        .Aggregate(
                                                                            (alpha, transpositionTable),
                                                                            (alpha_tt, move) =>
                                                                                alpha_tt.Item1 >= beta
                                                                                    ? (beta, alpha_tt.Item2.Append((board.ZobristKey, beta)).ToArray())
                                                                                    : Enumerable.Repeat(
                                                                                        new[] {
                                                                                            alpha_tt,
                                                                                            Enumerable.Repeat(board.MakeMove(move, board => search(board, depth - 1, alpha_tt.Item2, -beta, -alpha, shouldCancel)), 1)
                                                                                                .Select(score_tt => (-score_tt.Item1, score_tt.Item2))
                                                                                                .First()
                                                                                        }
                                                                                            .MaxBy(score_tt => score_tt.Item1)
                                                                                        , 1
                                                                                    )
                                                                                        .Select(score_tt => (score_tt.Item1, score_tt.Item2.Append((board.ZobristKey, score_tt.Item1)).ToArray()))
                                                                                        .First()

                                                                        )
                                                    ))(board, currentDepth, previousCheck.transpositionTable, int.MinValue + 1, int.MaxValue, () => timer.MillisecondsElapsedThisTurn > 1000)
                                                ),
                                                1
                                            )
                                                .Select(
                                                    score_tt => 
                                                        Enumerable.Repeat(
                                                            new[] {
                                                                (previousCheck.bestMove, previousCheck.scoreOfBestMove),
                                                                (move, -score_tt.Item1)
                                                            }
                                                                .MaxBy(move_score => move_score.Item2),
                                                            1
                                                        )
                                                            .Select(move_score => (move_score.Item1, move_score.Item2, score_tt.Item2))
                                                            .First()
                                                )
                                                .First()
                                    ),
                                1
                            )
                                .Select(
                                    move_score_tt => (
                                        depth: currentDepth,
                                        score: move_score_tt.Item2,
                                        move: move_score_tt.Item1,
                                        transpositionTable: move_score_tt.Item3,
                                        previousEvaluationMove: previousDepthResult.move
                                    )
                                )
                                .First()
            );

        Console.WriteLine($"Moves: {board.PlyCount} \t Score: {evaluation.score} \t Time Elapsed: {timer.MillisecondsElapsedThisTurn} \t Depth: {evaluation.depth}");
        
        return evaluation.previousEvaluationMove;
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