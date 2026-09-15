using System.Collections.Concurrent;
using Railway.Contracts;

namespace Railway.Messaging;

public interface IEventStore
{
    void Add(DomainEvent domainEvent);
    IReadOnlyList<DomainEvent> GetRecent(int count = 25);
}

public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentQueue<DomainEvent> _events = new();
    private const int MaxEvents = 200;

    public void Add(DomainEvent domainEvent)
    {
        _events.Enqueue(domainEvent);
        while (_events.Count > MaxEvents)
        {
            _events.TryDequeue(out _);
        }
    }

    public IReadOnlyList<DomainEvent> GetRecent(int count = 25) =>
        _events.Reverse().Take(count).ToArray();
}
