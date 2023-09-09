using System;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Example;
public class EvilBot : IChessBot
{
    public Move Think(Board board, Timer timer) =>
        board.GetLegalMoves()
            .MaxBy(move => 
                -board.MakeMove(move, board => 
                    board.GetAllPieceLists()
                        .Sum(pieceList =>
                            new[] { 0, 100, 300, 300, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                            * pieceList.Count
                            * (pieceList.IsWhitePieceList ? 1 : -1)
                        )
                    * (board.IsWhiteToMove ? 1 : -1)
                )
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