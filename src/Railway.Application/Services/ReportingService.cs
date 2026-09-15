using Railway.Contracts;
using Microsoft.EntityFrameworkCore;
using Railway.Persistence;

namespace Railway.Application.Services;

public sealed class ReportingService(RailwayDbContext db)
{
    public async Task<object> GetSummaryAsync()
    {
        var statusCounts = await db.Bookings
            .AsNoTracking()
            .GroupBy(booking => booking.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();

        // SQLite EF provider cannot Sum decimal in SQL; cast via double (SQL Server can Sum decimal directly).
        var revenue = (decimal)await db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Status == "CONFIRMED")
            .SumAsync(booking => (double)booking.Fare);

        var byClass = await db.Bookings
            .AsNoTracking()
            .GroupBy(booking => booking.TravelClass)
            .Select(group => new { Class = group.Key, Count = group.Count() })
            .ToListAsync();

        return new
        {
            TotalBookings = statusCounts.Sum(item => item.Count),
            ConfirmedBookings = statusCounts.FirstOrDefault(item => item.Status == "CONFIRMED")?.Count ?? 0,
            CancelledBookings = statusCounts.FirstOrDefault(item => item.Status == "CANCELLED")?.Count ?? 0,
            Revenue = revenue,
            ByClass = byClass
        };
    }
}
