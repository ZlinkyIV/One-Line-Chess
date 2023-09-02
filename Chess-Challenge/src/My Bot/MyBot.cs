using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int totalMovesSearched = 0;
    int movesMade = 0;

    public Move Think(Board board, Timer timer)
    {
        Move move = board.GetLegalMoves()
            .OrderByDescending(
                move => board.MakeMove(move, board => -NegaMax(board, 3))
            )
            .FirstOrDefault();

        movesMade += 1;

        Console.WriteLine($"Total: {totalMovesSearched} \t Moves: {movesMade} \t Average: {totalMovesSearched / movesMade}");
        
        return move;
    }

    int NegaMax(Board board, int depth) =>
        board.GetLegalMoves().Length == 0
            ? board.IsInCheckmate()
                ? -1000000
                : 0
            : depth == 0
                ? Evaluate(board)
                : board.GetLegalMoves()
                    .Select(move => board.MakeMove(move, board => -NegaMax(board, depth - 1)))
                    .Max();

    // int AlphaBeta(Board board, int depth, int alpha = int.MinValue + 1, int beta = int.MaxValue) =>
    //     board.GetLegalMoves().Length == 0
    //         ? board.IsInCheckmate()
    //             ? -1000000
    //             : 0
    //         : depth == 0
    //             ? Evaluate(board)
    //             : board.GetLegalMoves()
    //                 .Aggregate(alpha, (alpha, move) =>
    //                     alpha >= beta
    //                         ? beta
    //                         : Math.Max(
    //                             alpha,
    //                             board.MakeMove(move, board => -AlphaBeta(board, depth - 1, -beta, -alpha))
    //                         )
    //                 );

    int Evaluate(Board board)
    {
        totalMovesSearched += 1;

        return board.GetAllPieceLists()
            .Sum(pieceList =>
                new[] { 0, 100, 300, 300, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                * pieceList.Count
                * (pieceList.IsWhitePieceList ? 1 : -1)
            )
        * (board.IsWhiteToMove ? 1 : -1);
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