namespace Railway.Api.Services;

public sealed class NotificationService(ILogger<NotificationService> logger)
{
    public Task SendBookingConfirmationAsync(BookingDto booking, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending booking confirmation for PNR {Pnr} to traveller (simulated Email+SMS).",
            booking.Pnr);
        return Task.CompletedTask;
    }

    public Task SendCancellationAsync(BookingDto booking, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending cancellation notice for PNR {Pnr} to traveller (simulated Email+SMS).",
            booking.Pnr);
        return Task.CompletedTask;
    }
}
