using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // At some point — I can't remember when — I thought it a brilliant idea to
    // make a chess bot that was, not only smaller than 1024 tokens, took up only
    // a single statement.

    Func<Board, int, int, int, Func<bool>, int> search;

    // As you can see, the result is actually two statements (1.5 if you're kind)
    // because I forgot to realize that the search method I chose (traditional 
    // Alpha-Beta pruning w/ Quiecence) needs recursion to work. By that time, 
    // I didn't have time to start over with a non-recursive method, so I improvised!

    public Move Think(Board board, Timer timer) =>
        Enumerable.Range(0, 100)
            .Aggregate(
                (
                    nightly: (depth: 0, score: int.MinValue + 1, move: Move.NullMove),
                    stable: (depth: -1, score: int.MinValue + 1, move: Move.NullMove)
                ),
                (previousEvaluation, currentDepth) =>
                    timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / (60 - Math.Min(59, board.PlyCount))
                        ? previousEvaluation                                // Don't start a new search if time is up
                        : Enumerable.Repeat(
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
                                                                ? board.IsDraw() ? 0 : -1000000
                                                                : Enumerable.Repeat(
                                                                    depth <= 0                      // Don't calculate standpat/evaluation unless we have to
                                                                        ? (board.GetAllPieceLists()
                                                                            .Sum(pieceList =>
                                                                                new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                * pieceList.Count
                                                                                * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                            )
                                                                        + Enumerable.Repeat(
                                                                            (
                                                                                board.GetAllPieceLists(),
                                                                                1 - Math.Min(
                                                                                    1,
                                                                                        board.GetAllPieceLists()
                                                                                            .Sum(
                                                                                                pieceList => 
                                                                                                    new[] { 0, 0, 10, 10, 20, 45, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                                    * pieceList.Count
                                                                                            )
                                                                                        * 256 / 125
                                                                                    )
                                                                                
                                                                            ),
                                                                            1
                                                                        )
                                                                            .Select(
                                                                                pieceLists_endgameT => pieceLists_endgameT.Item1
                                                                                    .Sum(pieceList => pieceList.Sum(
                                                                                        piece => new Func<int, int, int>[] {
                                                                                            (x, y) => 0,
                                                                                            (x, y) => ((3 * (y - 1) * (8 - Math.Abs((x * 2) - 7))) * (256 - pieceLists_endgameT.Item2) + (20 * (y - 1)) * pieceLists_endgameT.Item2) / 256,
                                                                                            (x, y) => 30 - 3 * (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                                            (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                                            (x, y) => 0,
                                                                                            (x, y) => 0,
                                                                                            (x, y) => (((7 - y) * Math.Abs((x * 2) - 7) - 25) * (256 - pieceLists_endgameT.Item2) + (50 - 5 * (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7))) * pieceLists_endgameT.Item2) / 256,
                                                                                        }[(int)piece.PieceType](piece.IsWhite ? piece.Square.File : 7 - piece.Square.File, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank)
                                                                                    ) * (pieceList.IsWhitePieceList ? 1 : -1))
                                                                            )
                                                                            .First()
                                                                        ) * (board.IsWhiteToMove ? 1 : -1)
                                                                        : int.MinValue,                     // Don't calculate standpat/evaluation unless we have to
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
                                                                                        + Convert.ToInt32(move.IsCastles)
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
                                                    ))(board, currentDepth, int.MinValue + 1, int.MaxValue, () => timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / (60 - Math.Min(59, board.PlyCount)))
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
                                        stable: previousEvaluation.nightly      // We can now grantee "nightly" is not canceled partway through.
                                    )
                                )
                                .First()
            ).stable.move;
}

// ChessChallenge.API made this entire challenge much, much easier!
// However, there's no functional way of making and unmaking moves on
// a board. Since it's necessary for making a bot in one line possible, I
// decided to not count it against the "one" in "one-statement chess bot".
namespace ChessChallenge.API
{
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
}