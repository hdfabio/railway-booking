namespace Railway.Contracts;

public record StationDto(string Code, string Name, string City);

public record TrainSearchResult(
    Guid ScheduleId,
    Guid TrainId,
    string TrainNumber,
    string Name,
    StationDto Source,
    StationDto Destination,
    DateOnly TravelDate,
    TimeOnly Departure,
    TimeOnly Arrival,
    decimal BaseFare,
    IReadOnlyList<string> Classes,
    SeatAvailability? Availability);

public record SeatAvailability(
    Guid ScheduleId,
    DateOnly TravelDate,
    string Class,
    string Quota,
    int Confirmed,
    int Rac,
    int Waitlist,
    DateTime LastUpdated);

public record Passenger(string Name, int Age, string Gender, string? BerthPreference);

public record CreateBookingRequest(
    Guid ScheduleId,
    DateOnly TravelDate,
    string Class,
    string Quota,
    string PaymentMethod,
    IReadOnlyList<Passenger> Passengers);

public record BookingDto(
    string Pnr,
    Guid ScheduleId,
    DateOnly TravelDate,
    string Class,
    string Quota,
    IReadOnlyList<TicketPassenger> Passengers,
    decimal Fare,
    string PaymentStatus,
    string BookingStatus,
    DateTime CreatedAt);

public record TicketPassenger(string Name, int Age, string Gender, string Status, string? Coach, int? SeatNumber, string? Berth);

public record BookingResult(bool Success, string Message, BookingDto? Booking);

public record UserProfile(string UserId, string Name, string Email, string Phone, string LoyaltyTier);

public record LoginRequest(string Email, string Password);

public record TokenResponse(string AccessToken, DateTime ExpiresAt);

public record DomainEvent(string Type, string AggregateId, DateTime OccurredAt, object Payload);

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Railway";
    public string Audience { get; set; } = "Railway.Web";
    public int ExpiresMinutes { get; set; } = 60;
}

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "railway.events";
}

public static class BookingEventTypes
{
    public const string BookingCreated = "booking.created";
    public const string BookingCancelled = "booking.cancelled";
    public const string PaymentCaptured = "payment.captured";
    public const string NotificationBookingConfirmation = "notification.booking_confirmation";
    public const string NotificationBookingCancelled = "notification.booking_cancelled";
}
