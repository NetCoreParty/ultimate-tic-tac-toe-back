using UltimateTicTacToe.Core.Features.Game.Domain.Entities;

namespace UltimateTicTacToe.Core.Features.Game.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; }
    public virtual string EventName => GetType().Name;
    public int Version { get; protected set; } = 1;
    public DateTime OccurredOn { get; }

    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}

public record GameCreatedEvent(
    Guid GameId,
    Guid PlayerXId,
    Guid PlayerOId) : DomainEventBase;

public record CellMarkedEvent(
    Guid GameId,
    Guid PlayerId,
    int MiniBoardRowId, int MiniBoardColId,
    int CellRowId, int CellColId,
    PlayerFigure PlayerFigure) : DomainEventBase;

public record FullGameWonEvent(
    Guid GameId,
    Guid WinnerId) : DomainEventBase;

public record GameDrawnEvent(Guid GameId) : DomainEventBase;

public record MiniBoardWonEvent(
    Guid GameId,
    Guid WinnerId,
    int BoardRowId, int BoardColId,
    PlayerFigure PlayerFigure) : DomainEventBase;

public record MiniBoardDrawnEvent(
    Guid GameId,
    int BoardRowId, int BoardColId) : DomainEventBase;