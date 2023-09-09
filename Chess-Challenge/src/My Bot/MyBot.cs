using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer) =>
        board.GetLegalMoves()
            .MaxBy(move => 
                -board.MakeMove(move, board => 
                    Enumerable.Range(0, 63)
                        .Select(squareIndex => new Square(squareIndex))
                        .Sum(square => 
                            new[] { 0, 100, 300, 300, 500, 900, 0 } [(int)board.GetPiece(square).PieceType] 
                                * Convert.ToInt32(board.GetPiece(square).IsWhite)
                        )
                        * (board.IsWhiteToMove ? 1 : -1)

                    // board.GetAllPieceLists()
                    //     .Sum(pieceList =>
                    //         new[] { 0, 100, 300, 300, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                    //         * pieceList.Count
                    //         * (pieceList.IsWhitePieceList ? 1 : -1)
                    //     )
                    // * (board.IsWhiteToMove ? 1 : -1)
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