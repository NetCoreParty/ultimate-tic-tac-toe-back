namespace UltimateTicTacToe.Core.Configuration;

public class GameplaySettings
{
    public int EventsUntilSnapshot { get; set; }

    public int MaxActiveGames { get; set; }

    /// <summary>
    /// Backpressure threshold in percent of MaxActiveGames. Example: 90 means start rejecting new games at 90% capacity.
    /// </summary>
    public int BackpressureThresholdPercent { get; set; } = 90;
}