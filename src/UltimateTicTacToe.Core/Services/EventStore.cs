using UltimateTicTacToe.Core.Features.Game.Domain.Events;

namespace UltimateTicTacToe.Core.Services;

public interface IEventStore
{
    Task AppendEventsAsync(Guid gameAggregateId, IEnumerable<IDomainEvent> events);

    Task<List<IDomainEvent>> GetAllEvents(Guid gameAggregateId);

    Task<List<IDomainEvent>> GetEventsAfterVersion(Guid gameAggregateId, int version);
}

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<(int Version, IDomainEvent Event)>> _store = new();

    public Task AppendEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events)
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

    public Task<List<IDomainEvent>> GetAllEvents(Guid aggregateId)
    {
        if (_store.TryGetValue(aggregateId, out var events))
            return Task.FromResult(events.Select(e => e.Event).ToList());

        return Task.FromResult(new List<IDomainEvent>());
    }

    public Task<List<IDomainEvent>> GetEventsAfterVersion(Guid aggregateId, int version)
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
}