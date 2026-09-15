namespace Railway.Persistence.Domain;

public sealed class ApplicationUser
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LoyaltyTier { get; set; } = "Standard";
    public List<Booking> Bookings { get; set; } = [];
}

public sealed class Station
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public sealed class Train
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Schedule> Schedules { get; set; } = [];
    public List<Coach> Coaches { get; set; } = [];
}

public sealed class Schedule
{
    public Guid Id { get; set; }
    public Guid TrainId { get; set; }
    public Train Train { get; set; } = null!;
    public Guid SourceStationId { get; set; }
    public Station SourceStation { get; set; } = null!;
    public Guid DestinationStationId { get; set; }
    public Station DestinationStation { get; set; } = null!;
    public DateOnly TravelDate { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
    public List<Booking> Bookings { get; set; } = [];
}

public sealed class Coach
{
    public Guid Id { get; set; }
    public Guid TrainId { get; set; }
    public Train Train { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string TravelClass { get; set; } = string.Empty;
    public List<Seat> Seats { get; set; } = [];
}

public sealed class Seat
{
    public Guid Id { get; set; }
    public Guid CoachId { get; set; }
    public Coach Coach { get; set; } = null!;
    public int Number { get; set; }
    public string BerthType { get; set; } = string.Empty;
    public List<BookingPassenger> BookingPassengers { get; set; } = [];
}

public sealed class Booking
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;
    public Guid PnrRecordId { get; set; }
    public PnrRecord PnrRecord { get; set; } = null!;
    public string TravelClass { get; set; } = string.Empty;
    public string Quota { get; set; } = string.Empty;
    public string Status { get; set; } = "CONFIRMED";
    public decimal Fare { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BookingPassenger> Passengers { get; set; } = [];
    public Payment? Payment { get; set; }
    public Cancellation? Cancellation { get; set; }
}

public sealed class BookingPassenger
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid? SeatId { get; set; }
    public Seat? Seat { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BerthPreference { get; set; }
}

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
}

public sealed class Cancellation
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public DateTime CancelledAt { get; set; }
}

public sealed class PnrRecord
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Booking? Booking { get; set; }
}

public sealed class RacWaitlistQueueEntry
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;
    public Guid BookingPassengerId { get; set; }
    public BookingPassenger BookingPassenger { get; set; } = null!;
    public string TravelClass { get; set; } = string.Empty;
    public string Quota { get; set; } = string.Empty;
    public string QueueType { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
}
