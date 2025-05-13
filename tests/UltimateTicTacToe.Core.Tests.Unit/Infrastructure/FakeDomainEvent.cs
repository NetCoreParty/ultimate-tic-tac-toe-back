using UltimateTicTacToe.Core.Features.Game.Domain.Events;

namespace UltimateTicTacToe.Core.Tests.Unit.Infrastructure;

public class FakeDomainEvent : IDomainEvent
{
    public string Name { get; set; }

    public DateTime OccurredOn { get; set; }

    public int Version { get; set; }

    public FakeDomainEvent(string name, DateTime occurredOn)
    {
        Name = name;
        OccurredOn = occurredOn;
    }
}