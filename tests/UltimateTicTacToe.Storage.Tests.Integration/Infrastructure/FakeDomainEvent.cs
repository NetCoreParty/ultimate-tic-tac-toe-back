using UltimateTicTacToe.Core.Domain.Events;

namespace UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

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