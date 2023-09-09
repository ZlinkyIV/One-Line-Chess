using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        var evaluation = Enumerable.Range(1, 100)
            .Aggregate((
                    move: Move.NullMove,
                    score: 0,
                    depth: 0,
                    transpositionTable: Enumerable.Empty<(ulong, int, int)>(),
                    previousEvaluationMove: Move.NullMove
                ),
                (previousEvaluation, depth) => 
                    timer.MillisecondsElapsedThisTurn > 1000
                        ? previousEvaluation
                        : board.GetLegalMoves()
                            .Select(move => (
                                    move: move, 
                                    // score_tt: board.MakeMove(move, board => AlphaBeta(board, lastDepthMoveScore.transpositionTable, depth, () => false))
                                    score_tt: board.MakeMove(move, board => AlphaBeta(board, previousEvaluation.transpositionTable, depth, () => timer.MillisecondsElapsedThisTurn > 1000))
                                ))
                            .Select(evaluation => (
                                evaluation.move,
                                score: -evaluation.score_tt.score,
                                depth,
                                evaluation.score_tt.transpositionTable,
                                previousEvaluationMove: previousEvaluation.move
                            ))
                            .MaxBy(moveScore => moveScore.score)
            );

        Console.WriteLine($"Moves: {board.PlyCount} \t Score: {evaluation.score} \t Time Elapsed: {timer.MillisecondsElapsedThisTurn} \t Depth: {evaluation.depth}");
        
        return evaluation.previousEvaluationMove != Move.NullMove
            ? evaluation.previousEvaluationMove
            : evaluation.move;
    }

    (int score, IEnumerable<(ulong, int, int)> transpositionTable) AlphaBeta(Board board, IEnumerable<(ulong, int, int)> transpositionTable, int depth, Func<bool> shouldCancel, int alpha = int.MinValue + 1, int beta = int.MaxValue) =>
        board.GetLegalMoves().Length == 0
            ? (board.IsInCheckmate() ? -1000000 : 0, Enumerable.Empty<(ulong, int, int)>())
            : shouldCancel() || depth == 0
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
                )
                : board.GetLegalMoves()
                    .OrderByDescending(move =>
                        + transpositionTable.Aggregate((0ul, 0, 0), (maybeTheLineWeWant, currentLine) => currentLine.Item1 == board.ZobristKey ? currentLine : maybeTheLineWeWant).Item3
                        - 100 * Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                        + 100 * (move.CapturePieceType - move.MovePieceType)
                        + 100 * (int)move.PromotionPieceType
                    )
                    .Aggregate(
                        (score: alpha, transpositionTable: Enumerable.Empty<(ulong, int, int)>()),
                        (alpha_tt, move) =>
                            alpha_tt.score >= beta
                                ? (beta, alpha_tt.transpositionTable.Append((board.ZobristKey, depth, beta)))
                                : new[] {
                                    alpha_tt,
                                    new (int score, IEnumerable<(ulong, int, int)> transpositionTable)[] {
                                        board.MakeMove(move, board => AlphaBeta(board, transpositionTable, depth - 1, shouldCancel, -beta, -alpha))
                                    }
                                        .Select(score_tt => (
                                            score: -score_tt.score,
                                            transpositionTable: score_tt.transpositionTable.Append((board.ZobristKey, depth, score_tt.score))
                                        ))
                                        .First()
                                }
                                    .MaxBy(score_tt => score_tt.score)
                                
                    );
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