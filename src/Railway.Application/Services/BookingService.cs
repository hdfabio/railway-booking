using System.Data;
using Microsoft.EntityFrameworkCore;
using Railway.Application.Auth;
using Railway.Contracts;
using Railway.Messaging;
using Railway.Persistence;
using Railway.Persistence.Domain;

namespace Railway.Application.Services;

public sealed class BookingService(
    RailwayDbContext db,
    ICurrentUserService currentUser,
    ISeatInventoryService seatInventory,
    PaymentService payment,
    IEventBus eventBus)
{
    public async Task<BookingResult> CreateAsync(CreateBookingRequest request)
    {
        if (!currentUser.IsAuthenticated)
        {
            return new BookingResult(false, "Authentication required.", null);
        }

        if (request.Passengers.Count == 0)
        {
            return new BookingResult(false, "At least one passenger is required.", null);
        }

        var schedule = await db.Schedules
            .Include(item => item.Train)
                .ThenInclude(train => train.Coaches)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ScheduleId);
        if (schedule is null)
        {
            return new BookingResult(false, "Schedule not found for the selected journey date.", null);
        }

        if (request.TravelDate != schedule.TravelDate)
        {
            return new BookingResult(false, "Travel date does not match the selected schedule.", null);
        }

        if (!schedule.Train.Coaches.Any(coach => coach.TravelClass.Equals(request.Class, StringComparison.OrdinalIgnoreCase)))
        {
            return new BookingResult(false, $"Class {request.Class} is not available on {schedule.Train.Name}.", null);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var pnr = new PnrRecord
            {
                Id = Guid.NewGuid(),
                Number = await GeneratePnrAsync(),
                CurrentStatus = "CONFIRMED",
                CreatedAt = DateTime.UtcNow
            };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.UserId!.Value,
                ScheduleId = schedule.Id,
                PnrRecord = pnr,
                TravelClass = request.Class.ToUpperInvariant(),
                Quota = request.Quota.ToUpperInvariant(),
                Status = "CONFIRMED",
                Fare = CalculateFare(schedule.BaseFare, request.Class, request.Quota, request.Passengers.Count),
                CreatedAt = DateTime.UtcNow
            };

            booking.Passengers.AddRange(await seatInventory.ReserveSeatsAsync(request, booking));
            payment.AddPayment(booking, request.PaymentMethod);

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var dto = await MapBookingAsync(booking.Id);
            await eventBus.PublishAsync(BookingEventTypes.BookingCreated, dto.Pnr, dto);

            return new BookingResult(true, "Booking confirmed.", dto);
        }
        catch (SeatReservationException ex)
        {
            await transaction.RollbackAsync();
            return new BookingResult(false, ex.Message, null);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return new BookingResult(false, "One or more seats were just booked by another traveller. Please search again.", null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BookingResult> CancelAsync(string pnr)
    {
        if (!currentUser.IsAuthenticated)
        {
            return new BookingResult(false, "Authentication required.", null);
        }

        var normalizedPnr = pnr.Trim().ToUpperInvariant();
        var booking = await db.Bookings
            .Include(item => item.PnrRecord)
            .FirstOrDefaultAsync(item => item.PnrRecord.Number == normalizedPnr);
        if (booking is null)
        {
            return new BookingResult(false, "PNR not found.", null);
        }

        if (booking.UserId != currentUser.UserId)
        {
            return new BookingResult(false, "You are not allowed to cancel this booking.", null);
        }

        if (booking.Status == "CANCELLED")
        {
            return new BookingResult(true, "Booking was already cancelled.", await MapBookingAsync(booking.Id));
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            booking.Status = "CANCELLED";
            booking.PnrRecord.CurrentStatus = "CANCELLED";
            db.Cancellations.Add(new Cancellation
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Reason = "Traveller requested cancellation",
                RefundAmount = decimal.Round(booking.Fare * 0.8m, 2),
                CancelledAt = DateTime.UtcNow
            });

            await db.RacWaitlistQueue
                .Where(entry => entry.BookingPassenger.BookingId == booking.Id)
                .ExecuteDeleteAsync();

            await db.BookingPassengers
                .Where(passenger => passenger.BookingId == booking.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(passenger => passenger.SeatId, (Guid?)null)
                    .SetProperty(passenger => passenger.Status, "CANCELLED"));

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var dto = await MapBookingAsync(booking.Id);
            await eventBus.PublishAsync(BookingEventTypes.BookingCancelled, dto.Pnr, dto);

            return new BookingResult(true, "Booking cancelled.", dto);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BookingDto?> GetByPnrAsync(string pnr)
    {
        var normalizedPnr = pnr.Trim().ToUpperInvariant();
        var booking = await BookingsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.PnrRecord.Number == normalizedPnr);

        return booking is null ? null : MapToDto(booking);
    }

    public async Task<IReadOnlyList<BookingDto>> GetForUserAsync(Guid userId)
    {
        var bookings = await BookingsWithDetailsQuery()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync();

        return bookings.Select(MapToDto).ToArray();
    }

    public async Task<IReadOnlyList<BookingDto>> GetForCurrentUserAsync()
    {
        if (!currentUser.IsAuthenticated)
        {
            return [];
        }

        return await GetForUserAsync(currentUser.UserId!.Value);
    }

    private async Task<BookingDto> MapBookingAsync(Guid bookingId)
    {
        var booking = await BookingsWithDetailsQuery()
            .FirstAsync(item => item.Id == bookingId);

        return MapToDto(booking);
    }

    private IQueryable<Booking> BookingsWithDetailsQuery() =>
        db.Bookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Schedule)
            .Include(item => item.PnrRecord)
            .Include(item => item.Payment)
            .Include(item => item.Passengers)
                .ThenInclude(passenger => passenger.Seat)
                    .ThenInclude(seat => seat!.Coach);

    private static BookingDto MapToDto(Booking booking) =>
        new(
            booking.PnrRecord.Number,
            booking.ScheduleId,
            booking.Schedule.TravelDate,
            booking.TravelClass,
            booking.Quota,
            booking.Passengers.Select(passenger => new TicketPassenger(
                passenger.Name,
                passenger.Age,
                passenger.Gender,
                passenger.Status,
                passenger.Seat?.Coach.Code,
                passenger.Seat?.Number,
                passenger.Seat?.BerthType)).ToArray(),
            booking.Fare,
            booking.Payment?.Status ?? "PAYMENT_PENDING",
            booking.Status,
            booking.CreatedAt);

    private async Task<string> GeneratePnrAsync()
    {
        string value;
        do
        {
            value = Random.Shared.NextInt64(10_000_000_00, 99_999_999_99).ToString();
        }
        while (await db.Pnrs.AnyAsync(pnr => pnr.Number == value));

        return value;
    }

    private static decimal CalculateFare(decimal baseFare, string travelClass, string quota, int passengerCount)
    {
        var classMultiplier = travelClass.ToUpperInvariant() switch
        {
            "1AC" => 1.9m,
            "2AC" => 1.45m,
            "3AC" => 1.15m,
            "SL" => 0.55m,
            _ => 1m
        };
        var quotaMultiplier = quota.Equals("TATKAL", StringComparison.OrdinalIgnoreCase) ? 1.25m : 1m;
        return decimal.Round(baseFare * classMultiplier * quotaMultiplier * passengerCount, 2);
    }
}
