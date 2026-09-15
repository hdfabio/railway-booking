namespace Railway.Messaging;

public interface IEventBus
{
    Task PublishAsync(string eventType, string aggregateId, object payload, CancellationToken cancellationToken = default);
}
