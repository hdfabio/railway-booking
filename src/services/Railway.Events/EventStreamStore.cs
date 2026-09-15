using System.Collections.Concurrent;
using Railway.Contracts;

namespace Railway.Events;

public sealed class EventStreamStore
{
    private readonly ConcurrentQueue<DomainEvent> _events = new();
    private const int MaxEvents = 100;

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
