using UltimateTicTacToe.Core.Domain.Entities;

namespace UltimateTicTacToe.Core.Extensions;

public static class DomainExtensions
{
    public static PlayerFigure CheckWinner(this PlayerFigure[,] grid)
    {
        foreach (var line in GameConstants.WinLines)
        {
            var first = grid[line[0].Item1, line[0].Item2];

            if (first == PlayerFigure.None)
                continue;

            if (line.All(pos => grid[pos.Item1, pos.Item2] == first))
                return first;
        }

        return PlayerFigure.None;
    }

    public static bool IsFull(this PlayerFigure[,] grid)
    {
        foreach (var cell in grid)
        {
            if (cell == PlayerFigure.None)
                return false;
        }

        return true;
    }
}