namespace UltimateTicTacToe.Core.Configuration;

public class EventStoreSettings
{
    public string? Host { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string EventsCollectionName { get; set; } = null!;
}