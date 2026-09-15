using Microsoft.EntityFrameworkCore;
using Railway.Contracts;
using Railway.Persistence;
using Railway.Persistence.Domain;

namespace Railway.Application.Services;

/// <summary>Seat inventory with cache-aside reads (Redis/in-memory) and transactional writes.</summary>
public sealed class SeatInventoryService(RailwayDbContext db, RedisSeatAvailabilityCache cache) : ISeatInventoryService
{
    public async Task<SeatAvailability> GetAvailabilityAsync(Guid scheduleId, string travelClass, string quota)
    {
        var availabilityBySchedule = await GetAvailabilityForSchedulesAsync([scheduleId], travelClass, quota);
        return availabilityBySchedule[scheduleId];
    }

    public async Task<IReadOnlyDictionary<Guid, SeatAvailability>> GetAvailabilityForSchedulesAsync(
        IReadOnlyList<Guid> scheduleIds,
        string travelClass,
        string quota)
    {
        var normalizedClass = travelClass.ToUpperInvariant();
        var normalizedQuota = quota.ToUpperInvariant();
        var result = new Dictionary<Guid, SeatAvailability>();
        if (scheduleIds.Count == 0)
        {
            return result;
        }

        var missingScheduleIds = new List<Guid>();
        foreach (var scheduleId in scheduleIds.Distinct())
        {
            var cached = cache.Get(scheduleId, normalizedClass, normalizedQuota);
            if (cached is not null)
            {
                result[scheduleId] = cached;
            }
            else
            {
                missingScheduleIds.Add(scheduleId);
            }
        }

        if (missingScheduleIds.Count == 0)
        {
            return result;
        }

        var schedules = await db.Schedules
            .AsNoTracking()
            .Where(schedule => missingScheduleIds.Contains(schedule.Id))
            .Select(schedule => new { schedule.Id, schedule.TrainId, schedule.TravelDate })
            .ToListAsync();

        if (schedules.Count == 0)
        {
            return result;
        }

        var trainIds = schedules.Select(schedule => schedule.TrainId).Distinct().ToArray();

        var totalSeatsByTrain = await db.Seats
            .AsNoTracking()
            .Where(seat => trainIds.Contains(seat.Coach.TrainId) && seat.Coach.TravelClass == normalizedClass)
            .GroupBy(seat => seat.Coach.TrainId)
            .Select(group => new { TrainId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TrainId, item => item.Count);

        var confirmedBySchedule = await db.BookingPassengers
            .AsNoTracking()
            .Where(passenger =>
                missingScheduleIds.Contains(passenger.Booking.ScheduleId) &&
                passenger.Booking.TravelClass == normalizedClass &&
                passenger.Booking.Quota == normalizedQuota &&
                passenger.Booking.Status == "CONFIRMED" &&
                passenger.Status == "CNF" &&
                passenger.SeatId != null)
            .GroupBy(passenger => passenger.Booking.ScheduleId)
            .Select(group => new { ScheduleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ScheduleId, item => item.Count);

        var racBySchedule = await db.RacWaitlistQueue
            .AsNoTracking()
            .Where(entry =>
                missingScheduleIds.Contains(entry.ScheduleId) &&
                entry.TravelClass == normalizedClass &&
                entry.Quota == normalizedQuota &&
                entry.QueueType == "RAC")
            .GroupBy(entry => entry.ScheduleId)
            .Select(group => new { ScheduleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ScheduleId, item => item.Count);

        var waitlistBySchedule = await db.RacWaitlistQueue
            .AsNoTracking()
            .Where(entry =>
                missingScheduleIds.Contains(entry.ScheduleId) &&
                entry.TravelClass == normalizedClass &&
                entry.Quota == normalizedQuota &&
                entry.QueueType == "WL")
            .GroupBy(entry => entry.ScheduleId)
            .Select(group => new { ScheduleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ScheduleId, item => item.Count);

        foreach (var schedule in schedules)
        {
            totalSeatsByTrain.TryGetValue(schedule.TrainId, out var totalSeats);
            confirmedBySchedule.TryGetValue(schedule.Id, out var confirmedSeats);
            racBySchedule.TryGetValue(schedule.Id, out var rac);
            waitlistBySchedule.TryGetValue(schedule.Id, out var waitlist);

            var availability = new SeatAvailability(
                schedule.Id,
                schedule.TravelDate,
                normalizedClass,
                normalizedQuota,
                totalSeats - confirmedSeats,
                rac,
                waitlist,
                DateTime.UtcNow);

            cache.Set(availability);
            result[schedule.Id] = availability;
        }

        return result;
    }

    /// <summary>
    /// Must run inside the caller's database transaction (Serializable) to prevent double booking.
    /// </summary>
    public async Task<IReadOnlyList<BookingPassenger>> ReserveSeatsAsync(CreateBookingRequest request, Booking booking)
    {
        var travelClass = request.Class.ToUpperInvariant();
        var bookedSeatIds = await db.BookingPassengers
            .Where(passenger =>
                passenger.Booking.ScheduleId == request.ScheduleId &&
                passenger.Booking.TravelClass == travelClass &&
                passenger.Booking.Status == "CONFIRMED" &&
                passenger.Status == "CNF" &&
                passenger.SeatId != null)
            .Select(passenger => passenger.SeatId!.Value)
            .ToListAsync();

        var seats = await db.Seats
            .Include(seat => seat.Coach)
            .Where(seat => seat.Coach.Train.Schedules.Any(schedule => schedule.Id == request.ScheduleId))
            .Where(seat => seat.Coach.TravelClass == travelClass)
            .Where(seat => !bookedSeatIds.Contains(seat.Id))
            .OrderBy(seat => seat.Coach.Code)
            .ThenBy(seat => seat.Number)
            .ToListAsync();

        var currentRac = await NextQueuePositionAsync(request.ScheduleId, travelClass, request.Quota, "RAC");
        var currentWaitlist = await NextQueuePositionAsync(request.ScheduleId, travelClass, request.Quota, "WL");
        var passengerEntities = new List<BookingPassenger>();

        foreach (var passenger in PrioritizePassengers(request.Passengers))
        {
            var preferredSeat = seats.FirstOrDefault(seat => seat.BerthType.Equals(ChooseBerth(passenger), StringComparison.OrdinalIgnoreCase))
                ?? seats.FirstOrDefault();
            if (preferredSeat is not null)
            {
                var seatStillFree = !await db.BookingPassengers.AnyAsync(existing =>
                    existing.SeatId == preferredSeat.Id &&
                    existing.Booking.ScheduleId == request.ScheduleId &&
                    existing.Booking.Status == "CONFIRMED" &&
                    existing.Status == "CNF");
                if (!seatStillFree)
                {
                    seats.Remove(preferredSeat);
                    preferredSeat = seats.FirstOrDefault();
                }

                if (preferredSeat is null)
                {
                    throw new SeatReservationException("No confirmed seats remain for this train.");
                }

                seats.Remove(preferredSeat);
                passengerEntities.Add(new BookingPassenger
                {
                    Id = Guid.NewGuid(),
                    Booking = booking,
                    SeatId = preferredSeat.Id,
                    Name = passenger.Name,
                    Age = passenger.Age,
                    Gender = passenger.Gender,
                    Status = "CNF",
                    BerthPreference = passenger.BerthPreference
                });
                continue;
            }

            var status = currentRac <= 30 ? $"RAC {currentRac}" : $"WL {currentWaitlist}";
            var queueType = currentRac <= 30 ? "RAC" : "WL";
            var position = currentRac <= 30 ? currentRac++ : currentWaitlist++;

            var queuedPassenger = new BookingPassenger
            {
                Id = Guid.NewGuid(),
                Booking = booking,
                Name = passenger.Name,
                Age = passenger.Age,
                Gender = passenger.Gender,
                Status = status,
                BerthPreference = passenger.BerthPreference
            };

            passengerEntities.Add(queuedPassenger);
            db.RacWaitlistQueue.Add(new RacWaitlistQueueEntry
            {
                Id = Guid.NewGuid(),
                ScheduleId = request.ScheduleId,
                BookingPassenger = queuedPassenger,
                TravelClass = travelClass,
                Quota = request.Quota.ToUpperInvariant(),
                QueueType = queueType,
                Position = position,
                CreatedAt = DateTime.UtcNow
            });
        }

        cache.Invalidate(request.ScheduleId, travelClass, request.Quota);
        return passengerEntities;
    }

    public async Task RefreshAvailabilityAsync(
        Guid scheduleId,
        string travelClass,
        string quota,
        CancellationToken cancellationToken = default)
    {
        cache.Invalidate(scheduleId, travelClass, quota);
        _ = await GetAvailabilityAsync(scheduleId, travelClass, quota);
    }

    private static IEnumerable<Passenger> PrioritizePassengers(IEnumerable<Passenger> passengers) =>
        passengers.OrderByDescending(passenger => passenger.Age >= 60 || passenger.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
            .ThenBy(passenger => passenger.Name);

    private static string ChooseBerth(Passenger passenger)
    {
        if (passenger.Age >= 60 || passenger.BerthPreference?.Equals("Lower", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Lower";
        }

        return passenger.BerthPreference is { Length: > 0 } ? passenger.BerthPreference : "Side Upper";
    }

    private async Task<int> NextQueuePositionAsync(Guid scheduleId, string travelClass, string quota, string queueType)
    {
        var normalizedClass = travelClass.ToUpperInvariant();
        var normalizedQuota = quota.ToUpperInvariant();
        var latest = await db.RacWaitlistQueue
            .Where(entry => entry.ScheduleId == scheduleId && entry.TravelClass == normalizedClass && entry.Quota == normalizedQuota && entry.QueueType == queueType)
            .Select(entry => (int?)entry.Position)
            .MaxAsync();

        return (latest ?? 0) + 1;
    }
}
