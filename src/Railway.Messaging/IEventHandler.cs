using Railway.Contracts;

namespace Railway.Messaging;

public interface IEventHandler
{
    bool CanHandle(string eventType);
    Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
