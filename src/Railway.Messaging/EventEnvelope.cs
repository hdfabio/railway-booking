using System.Text.Json;
using Railway.Contracts;

namespace Railway.Messaging;

internal sealed record EventEnvelope(string Type, string AggregateId, DateTime OccurredAt, JsonElement Payload);

internal static class EventEnvelopeMapper
{
    public static string Serialize(string eventType, string aggregateId, object payload) =>
        JsonSerializer.Serialize(new EventEnvelope(eventType, aggregateId, DateTime.UtcNow, JsonSerializer.SerializeToElement(payload)));

    public static DomainEvent ToDomainEvent(string json)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json)
            ?? throw new InvalidOperationException("Invalid event envelope.");

        object payload = envelope.Type switch
        {
            BookingEventTypes.BookingCreated or BookingEventTypes.BookingCancelled
                => envelope.Payload.Deserialize<BookingDto>()!,
            _ => envelope.Payload
        };

        return new DomainEvent(envelope.Type, envelope.AggregateId, envelope.OccurredAt, payload);
    }
}
