using Microsoft.Extensions.DependencyInjection;
using Railway.Application.Services;
using Railway.Contracts;
using Railway.Messaging;

namespace Railway.Application.EventHandlers;

public sealed class NotificationEventHandler(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : IEventHandler
{
    public bool CanHandle(string eventType) =>
        eventType is BookingEventTypes.BookingCreated or BookingEventTypes.BookingCancelled;

    public async Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        switch (domainEvent.Type)
        {
            case BookingEventTypes.BookingCreated when domainEvent.Payload is BookingDto created:
                await notifications.SendBookingConfirmationAsync(created, cancellationToken);
                await eventBus.PublishAsync(
                    BookingEventTypes.NotificationBookingConfirmation,
                    created.Pnr,
                    new { created.Pnr, Channel = "Email+SMS" },
                    cancellationToken);
                break;

            case BookingEventTypes.BookingCancelled when domainEvent.Payload is BookingDto cancelled:
                await notifications.SendCancellationAsync(cancelled, cancellationToken);
                await eventBus.PublishAsync(
                    BookingEventTypes.NotificationBookingCancelled,
                    cancelled.Pnr,
                    new { cancelled.Pnr, Channel = "Email+SMS" },
                    cancellationToken);
                break;
        }
    }
}
