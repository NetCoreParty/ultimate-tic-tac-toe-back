using UltimateTicTacToe.Core.Features.Game.Domain.Events;

namespace UltimateTicTacToe.Core.Features.Gameplay;

public interface IEventStore
{
    Task AppendEventsAsync(Guid gameAggregateId, IEnumerable<IDomainEvent> events, CancellationToken ct = default);

    Task<List<IDomainEvent>> GetAllEventsAsync(Guid gameAggregateId, CancellationToken ct = default);

    Task<List<IDomainEvent>> GetEventsAfterVersionAsync(Guid gameAggregateId, int version, CancellationToken ct = default);

    Task DeleteEventsByAsync(Guid gameAggregateId, CancellationToken ct = default);
}

public class MongoIndexInfo
{
    public string Name { get; set; } = default!;
    public Dictionary<string, int> KeyMap { get; set; } = new(); // e.g., { "AggregateId": 1, "Version": -1 }
    public bool IsUnique { get; set; }

    public override string ToString() =>
        $"Name: {Name}, Unique: {IsUnique}, Keys: [{string.Join(", ", KeyMap.Select(k => $"{k.Key}:{k.Value}"))}]";
}

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<(int Version, IDomainEvent Event)>> _store = new();

    public Task AppendEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        if (!_store.ContainsKey(aggregateId))
            _store[aggregateId] = new();

        var versionStart = _store[aggregateId].Count;

        int version = versionStart;
        foreach (var e in events)
        {
            _store[aggregateId].Add((++version, e));
        }

        return Task.CompletedTask;
    }

    public Task<List<IDomainEvent>> GetAllEventsAsync(Guid aggregateId, CancellationToken ct = default)
    {
        if (_store.TryGetValue(aggregateId, out var events))
            return Task.FromResult(events.Select(e => e.Event).ToList());

        return Task.FromResult(new List<IDomainEvent>());
    }

    public Task<List<IDomainEvent>> GetEventsAfterVersionAsync(Guid aggregateId, int version, CancellationToken ct = default)
    {
        if (!_store.ContainsKey(aggregateId))
            return Task.FromResult(new List<IDomainEvent>());

        return Task.FromResult(
            _store[aggregateId]
                .Where(e => e.Version > version)
                .Select(e => e.Event)
                .ToList()
        );
    }

    public Task DeleteEventsByAsync(Guid aggregateId, CancellationToken ct = default)
    {
        if (_store.ContainsKey(aggregateId))
            _store.Remove(aggregateId);

        return Task.CompletedTask;
    }
}