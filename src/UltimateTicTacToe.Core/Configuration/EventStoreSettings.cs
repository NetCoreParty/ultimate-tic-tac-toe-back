namespace UltimateTicTacToe.Core.Configuration;

public class EventStoreSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string EventsCollectionName { get; set; } = null!;
}