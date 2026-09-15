using Railway.Api.Data;
using Railway.Api.Domain;

namespace Railway.Api.Services;

public sealed class PaymentService(RailwayDbContext db, ILogger<PaymentService> logger)
{
    public void AddPayment(Booking booking, string method)
    {
        var status = string.IsNullOrWhiteSpace(method) ? "PAYMENT_PENDING" : "PAID";
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = booking.Fare,
            Method = method,
            Status = status,
            PaidAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        booking.Payment = payment;
    }

    public void LogCaptured(BookingDto booking) =>
        logger.LogInformation(
            "Payment gateway captured funds for PNR {Pnr}. Amount: {Amount}, Status: {Status}",
            booking.Pnr,
            booking.Fare,
            booking.PaymentStatus);
}
