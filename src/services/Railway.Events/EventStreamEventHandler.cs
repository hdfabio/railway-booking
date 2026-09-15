using Railway.Contracts;
using Railway.Messaging;

namespace Railway.Events;

public sealed class EventStreamEventHandler(EventStreamStore store) : IEventHandler
{
    public bool CanHandle(string eventType) => true;

    public Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        store.Add(domainEvent);
        return Task.CompletedTask;
    }
}
