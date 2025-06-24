using UltimateTicTacToe.Core.Domain.Entities;

namespace UltimateTicTacToe.Core.Domain.Events;

public interface IDomainEvent
{
    int Version { get; set; }
    DateTime OccurredOn { get; set; }
}
public abstract record DomainEventBase : IDomainEvent
{
    public virtual string EventName => GetType().Name;
    public DateTime OccurredOn { get; set; }
    public int Version { get; set; }

    protected DomainEventBase()
    {
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