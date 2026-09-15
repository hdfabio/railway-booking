using Microsoft.Extensions.DependencyInjection;
using Railway.Application.Services;
using Railway.Contracts;
using Railway.Messaging;

namespace Railway.Application.EventHandlers;

public sealed class SeatInventoryEventHandler(IServiceScopeFactory scopeFactory) : IEventHandler
{
    public bool CanHandle(string eventType) =>
        eventType is BookingEventTypes.BookingCreated or BookingEventTypes.BookingCancelled;

    public async Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Payload is not BookingDto booking)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var inventory = scope.ServiceProvider.GetRequiredService<ISeatInventoryService>();
        await inventory.RefreshAvailabilityAsync(
            booking.ScheduleId,
            booking.Class,
            booking.Quota,
            cancellationToken);
    }
}
