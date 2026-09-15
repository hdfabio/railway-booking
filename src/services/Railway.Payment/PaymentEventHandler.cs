using Railway.Application.Services;
using Railway.Contracts;
using Railway.Messaging;

namespace Railway.Payment;

public sealed class PaymentEventHandler(IServiceScopeFactory scopeFactory) : IEventHandler
{
    public bool CanHandle(string eventType) => eventType == BookingEventTypes.BookingCreated;

    public async Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Payload is not BookingDto booking)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<PaymentService>().LogCaptured(booking);
    }
}
