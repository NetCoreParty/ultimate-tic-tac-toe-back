namespace UltimateTicTacToe.Core.Extensions;

public static class GameConstants
{
    // Each tuple is (row, col) of one cell in a line
    public static readonly List<(int, int)[]> WinLines = new()
    {
        // Rows
        new[] { (0, 0), (0, 1), (0, 2) },
        new[] { (1, 0), (1, 1), (1, 2) },
        new[] { (2, 0), (2, 1), (2, 2) },

        // Columns
        new[] { (0, 0), (1, 0), (2, 0) },
        new[] { (0, 1), (1, 1), (2, 1) },
        new[] { (0, 2), (1, 2), (2, 2) },

        // Diagonals
        new[] { (0, 0), (1, 1), (2, 2) },
        new[] { (0, 2), (1, 1), (2, 0) }
    };
}