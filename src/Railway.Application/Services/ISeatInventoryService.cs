using Railway.Contracts;
using Railway.Persistence.Domain;

namespace Railway.Application.Services;

public interface ISeatInventoryService
{
    Task<SeatAvailability> GetAvailabilityAsync(Guid scheduleId, string travelClass, string quota);
    Task<IReadOnlyDictionary<Guid, SeatAvailability>> GetAvailabilityForSchedulesAsync(
        IReadOnlyList<Guid> scheduleIds,
        string travelClass,
        string quota);
    Task<IReadOnlyList<BookingPassenger>> ReserveSeatsAsync(CreateBookingRequest request, Booking booking);
    Task RefreshAvailabilityAsync(
        Guid scheduleId,
        string travelClass,
        string quota,
        CancellationToken cancellationToken = default);
}
