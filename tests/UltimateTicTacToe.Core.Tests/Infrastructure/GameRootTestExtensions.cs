using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;

namespace UltimateTicTacToe.Core.Tests.Infrastructure;

internal static class GameRootTestExtensions
{
    internal static void ForceSetStatus(this GameRoot game, GameStatus status)
    {
        var x = game.PlayerXId;
        var o = game.PlayerOId;

        if (status == GameStatus.WON)
            SimulateFullWin(game, x, o);
        else if (status == GameStatus.DRAW)
            SimulateFullDraw(game, x, o);
        else
            throw new NotImplementedException($"Status {status} is not implemented in test extension");
    }

    internal static void SimulateMiniBoardWin(this GameRoot gameRoot, Guid winner, Guid opponent)
    {
        // Player wins top row in MiniBoard (0,0)
        gameRoot.PlayMove(winner, 0, 0, 0, 0);
        gameRoot.PlayMove(opponent, 0, 0, 1, 0);
        gameRoot.PlayMove(winner, 0, 0, 0, 1);
        gameRoot.PlayMove(opponent, 0, 0, 1, 1);
        gameRoot.PlayMove(winner, 0, 0, 0, 2); // Player wins mini board (0,0)
    }

    internal static void ForceSetWinner(this GameRoot game, Guid winnerId)
    {
        var x = game.PlayerXId;
        var o = game.PlayerOId;

        if (winnerId == x)
            SimulateFullWin(game, futureWinner: x, o);
        else if (winnerId == o)
            SimulateFullWin(game, futureWinner: o, x);
    }

    internal static void ForceMiniBoardDraw(this GameRoot game, int miniBoardRow, int miniBoardCol)
    {
        FillMiniBoardDraw(game, miniBoardRow, miniBoardCol, game.PlayerXId, game.PlayerOId);
    }

    private static void SimulateFullWin(this GameRoot gameRoot, Guid futureWinner, Guid loser)
    {
        // Player wins top row in mini board (0,0)
        gameRoot.PlayMove(futureWinner, 0, 0, 0, 0);
        gameRoot.PlayMove(loser, 0, 0, 1, 0);
        gameRoot.PlayMove(futureWinner, 0, 0, 0, 1);
        gameRoot.PlayMove(loser, 0, 0, 1, 1);
        gameRoot.PlayMove(futureWinner, 0, 0, 0, 2); // Player Win MiniBoard (0,0)

        // Player wins top row of BigBoard by winning (0,1) and (0,2) as well
        gameRoot.PlayMove(loser, 1, 0, 0, 0);
        gameRoot.PlayMove(futureWinner, 0, 1, 0, 0); // Player starts winning next MiniBoard
        gameRoot.PlayMove(loser, 1, 1, 0, 0);
        gameRoot.PlayMove(futureWinner, 0, 1, 0, 1);
        gameRoot.PlayMove(loser, 1, 1, 0, 1);
        gameRoot.PlayMove(futureWinner, 0, 1, 0, 2); // Player Win MiniBoard (0,1)

        gameRoot.PlayMove(loser, 1, 2, 0, 0);
        gameRoot.PlayMove(futureWinner, 0, 2, 0, 0); // Player starts winning next MiniBoard
        gameRoot.PlayMove(loser, 1, 2, 0, 1);
        gameRoot.PlayMove(futureWinner, 0, 2, 0, 1);
        gameRoot.PlayMove(loser, 1, 2, 0, 2);
        gameRoot.PlayMove(futureWinner, 0, 2, 0, 2); // Player Win MiniBoard (0,2)

        // Now Player should have won top row of BigBoard (0,0), (0,1), (0,2)
    }

    private static void SimulateFullDraw(this GameRoot gameRoot, Guid playerX, Guid playerO)
    {
        // Fill each mini board in draw pattern one by one
        FillMiniBoardDraw(gameRoot, 0, 0, playerX, playerO);
        // Now we should swap players to fill the next mini board and so on
        FillMiniBoardDraw(gameRoot, 0, 1, playerO, playerX);
        FillMiniBoardDraw(gameRoot, 0, 2, playerX, playerO);
        FillMiniBoardDraw(gameRoot, 1, 0, playerO, playerX);
        FillMiniBoardDraw(gameRoot, 1, 1, playerX, playerO);
        FillMiniBoardDraw(gameRoot, 1, 2, playerO, playerX);
        FillMiniBoardDraw(gameRoot, 2, 0, playerX, playerO);
        FillMiniBoardDraw(gameRoot, 2, 1, playerO, playerX);
        FillMiniBoardDraw(gameRoot, 2, 2, playerX, playerO);
    }

    private static void FillMiniBoardDraw(GameRoot gameRoot, int miniRow, int miniCol, Guid playerOne, Guid playerTwo)
    {
        // Order of moves to avoid any 3-in-a-row

        /* 
           X O X 
           X X O
           O X O
        */

        var moves = new (Guid player, int row, int col)[]
        {
            (playerOne, 0, 0), (playerTwo, 0, 1),
            (playerOne, 0, 2), (playerTwo, 2, 0),
            (playerOne, 1, 0), (playerTwo, 2, 2),
            (playerOne, 1, 1), (playerTwo, 1, 2),
            (playerOne, 2, 1)
        };

        foreach (var move in moves)
        {
            gameRoot.PlayMove(move.player, miniRow, miniCol, move.row, move.col);
        }
    }
}