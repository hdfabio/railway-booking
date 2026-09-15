using Railway.Contracts;
using Microsoft.EntityFrameworkCore;
using Railway.Persistence;
using Railway.Persistence.Domain;

namespace Railway.Application.Services;

public sealed class TrainScheduleService(RailwayDbContext db)
{
    public async Task<IReadOnlyList<TrainSearchResult>> SearchAsync(string source, string destination, DateOnly date)
    {
        var query = db.Schedules
            .AsNoTracking()
            .Include(schedule => schedule.Train)
                .ThenInclude(train => train.Coaches)
            .Include(schedule => schedule.SourceStation)
            .Include(schedule => schedule.DestinationStation)
            .Where(schedule => schedule.TravelDate == date);

        query = ApplyStationFilter(query, source, matchSource: true);
        query = ApplyStationFilter(query, destination, matchSource: false);

        var schedules = await query.ToListAsync();

        return schedules.Select(schedule => new TrainSearchResult(
            schedule.Id,
            schedule.TrainId,
            schedule.Train.Number,
            schedule.Train.Name,
            new StationDto(schedule.SourceStation.Code, schedule.SourceStation.Name, schedule.SourceStation.City),
            new StationDto(schedule.DestinationStation.Code, schedule.DestinationStation.Name, schedule.DestinationStation.City),
            schedule.TravelDate,
            schedule.DepartureTime,
            schedule.ArrivalTime,
            schedule.BaseFare,
            schedule.Train.Coaches.Select(coach => coach.TravelClass).Distinct().Order().ToArray(),
            null)).ToArray();
    }

    public async Task<TrainSearchResult?> GetByScheduleIdAsync(Guid scheduleId)
    {
        return (await SearchByScheduleIdAsync(scheduleId)).FirstOrDefault();
    }

    private static IQueryable<Schedule> ApplyStationFilter(IQueryable<Schedule> query, string term, bool matchSource)
    {
        var trimmed = term.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return query;
        }

        if (StationSearch.IsStationCode(trimmed))
        {
            var code = StationSearch.NormalizeCode(trimmed);
            return matchSource
                ? query.Where(schedule => schedule.SourceStation.Code == code)
                : query.Where(schedule => schedule.DestinationStation.Code == code);
        }

        var pattern = StationSearch.ToLikePattern(trimmed);
        var codeGuess = StationSearch.NormalizeCode(trimmed);
        return matchSource
            ? query.Where(schedule =>
                schedule.SourceStation.Code == codeGuess ||
                EF.Functions.Like(schedule.SourceStation.City, pattern) ||
                EF.Functions.Like(schedule.SourceStation.Name, pattern))
            : query.Where(schedule =>
                schedule.DestinationStation.Code == codeGuess ||
                EF.Functions.Like(schedule.DestinationStation.City, pattern) ||
                EF.Functions.Like(schedule.DestinationStation.Name, pattern));
    }

    private async Task<IReadOnlyList<TrainSearchResult>> SearchByScheduleIdAsync(Guid scheduleId)
    {
        var schedule = await db.Schedules
            .AsNoTracking()
            .Include(item => item.Train)
                .ThenInclude(train => train.Coaches)
            .Include(item => item.SourceStation)
            .Include(item => item.DestinationStation)
            .FirstOrDefaultAsync(item => item.Id == scheduleId);

        if (schedule is null)
        {
            return [];
        }

        return
        [
            new TrainSearchResult(
                schedule.Id,
                schedule.TrainId,
                schedule.Train.Number,
                schedule.Train.Name,
                new StationDto(schedule.SourceStation.Code, schedule.SourceStation.Name, schedule.SourceStation.City),
                new StationDto(schedule.DestinationStation.Code, schedule.DestinationStation.Name, schedule.DestinationStation.City),
                schedule.TravelDate,
                schedule.DepartureTime,
                schedule.ArrivalTime,
                schedule.BaseFare,
                schedule.Train.Coaches.Select(coach => coach.TravelClass).Distinct().Order().ToArray(),
                null)
        ];
    }
}
