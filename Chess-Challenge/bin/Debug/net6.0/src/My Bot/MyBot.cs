using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    Func<int, int> factorial;
    public int TestFunction() =>
        (factorial = new(x => x == 0 ? 1 : x * factorial(x - 1)))(10);

Func<Board, int, IEnumerable<(ulong, int, int)>, int, int, Func<bool>, (int, IEnumerable<(ulong, int, int)>)> search;
    public Move Think(Board board, Timer timer)
    {
        var evaluation = Enumerable.Range(0, 100)
            .Aggregate(
                (
                    depth: 0,
                    score: 0,
                    move: Move.NullMove,
                    transpositionTable: Enumerable.Empty<(ulong, int, int)>()
                ),
                (previousEvaluation, currentDepth) =>
                    timer.MillisecondsElapsedThisTurn > 1000                    // \
                        ? previousEvaluation                                    //  | Don't start a new search if time is up
                        : board.GetLegalMoves()                                 // /
                            .Select(
                                move => (
                                    move,
                                    board.MakeMove(move, board => 
                                        (search = new(
                                            (board, depth, transpositionTable, alpha, beta, shouldCancel) => 
                                                board.GetLegalMoves().Length == 0
                                                    ? (board.IsInCheckmate() ? -1000000 : 0, transpositionTable)
                                                    : depth == 0 || shouldCancel()
                                                        ? (
                                                            (board.GetAllPieceLists()
                                                                .Sum(pieceList =>
                                                                    new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                    * pieceList.Count
                                                                    * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                )
                                                            + board.GetAllPieceLists()
                                                                .Sum(pieceList => pieceList.Sum(
                                                                    piece => new Func<int, int, int>[] {
                                                                        (x, y) => 0,
                                                                        (x, y) => y == 6 ? 50 : 0, // + Convert.ToInt32(x == 3 || x == 4) * new[] { 0, -20, 0, 20, 0, 0, 0, 0 }[y],
                                                                        (x, y) => 20 - (int)(Math.Pow(x * 2 - 7, 2) + Math.Pow(y * 2 - 7, 2)),
                                                                        (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                        (x, y) => 0,
                                                                        (x, y) => 0,
                                                                        (x, y) => (Math.Abs(x * 2 - 7) / 2 - y * 2) * 8
                                                                    } [(int)piece.PieceType]((piece.IsWhite ? 1 : -1) * piece.Square.Rank + (piece.IsWhite ? 1 : 7), (piece.IsWhite ? 1 : -1) * piece.Square.File + (piece.IsWhite ? 1 : 7))
                                                                ))
                                                            ) * (board.IsWhiteToMove ? 1 : -1), 
                                                            Enumerable.Empty<(ulong, int, int)>()
                                                            // transpositionTable.Append((board.ZobristKey, depth, ))
                                                        )
                                                        : board.GetLegalMoves()
                                                            .OrderByDescending(move =>
                                                                transpositionTable.Where(transposition => transposition.Item1 == board.ZobristKey).Count() * 1000
                                                                - 100 * Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                                                                + 100 * (move.CapturePieceType - move.MovePieceType)
                                                                + 100 * (int)move.PromotionPieceType
                                                            )
                                                            .Aggregate(
                                                                (alpha, Enumerable.Empty<(ulong, int, int)>()),
                                                                (alpha_tt, move) =>
                                                                    alpha_tt.Item1 >= beta
                                                                        ? (beta, alpha_tt.Item2.Append((board.ZobristKey, depth, beta)))
                                                                        : new[] {
                                                                            alpha_tt,
                                                                            new (int, IEnumerable<(ulong, int, int)>)[] {
                                                                                board.MakeMove(move, board => search(board, depth - 1, transpositionTable, -beta, -alpha, shouldCancel))
                                                                            }
                                                                                .Select(score_tt => (
                                                                                    -score_tt.Item1,
                                                                                    score_tt.Item2.Append((board.ZobristKey, depth, score_tt.Item1))
                                                                                ))
                                                                                .First()
                                                                        }
                                                                            .MaxBy(score_tt => score_tt.Item1)
                                                            )
                                        ))(board, currentDepth, previousEvaluation.transpositionTable, int.MinValue + 1, int.MaxValue, () => timer.MillisecondsElapsedThisTurn > 1000)
                                    )
                                )
                            )
                            .Select(
                                searchResults => (
                                    depth: currentDepth,
                                    score: -searchResults.Item2.Item1,
                                    move: searchResults.Item1,
                                    transpositionTable: searchResults.Item2.Item2
                                )
                            )
                            .MaxBy(evaluation => evaluation.score)
            );

        Console.WriteLine($"Moves: {board.PlyCount} \t Score: {evaluation.score} \t Time Elapsed: {timer.MillisecondsElapsedThisTurn} \t Depth: {evaluation.depth}");
        
        return evaluation.move;
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