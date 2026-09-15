namespace Railway.Api.Services;

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
