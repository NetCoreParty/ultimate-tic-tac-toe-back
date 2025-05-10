using UltimateTicTacToe.Core.Features.Game.Domain.Events;

namespace UltimateTicTacToe.Core.Tests.Infrastructure;

public class FakeDomainEvent : IDomainEvent
{
    public string Name { get; set; }

    public DateTime OccurredOn { get; init; }

    public FakeDomainEvent(string name, DateTime occurredOn)
    {
        Name = name;
        OccurredOn = occurredOn;
    }
}