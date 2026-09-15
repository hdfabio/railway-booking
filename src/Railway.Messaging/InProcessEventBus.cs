using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Railway.Contracts;

namespace Railway.Messaging;

public sealed class InProcessEventBus(
    IServiceScopeFactory scopeFactory,
    IEventStore eventStore,
    ILogger<InProcessEventBus> logger) : IEventBus
{
    public async Task PublishAsync(
        string eventType,
        string aggregateId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var domainEvent = new DomainEvent(eventType, aggregateId, DateTime.UtcNow, payload);
        eventStore.Add(domainEvent);

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider
            .GetServices<IEventHandler>()
            .Where(handler => handler.CanHandle(eventType))
            .ToArray();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "In-process handler {Handler} failed for {EventType}.", handler.GetType().Name, eventType);
                throw;
            }
        }
    }
}
