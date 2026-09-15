using System.Collections.Concurrent;

namespace Railway.Api.Services;

public sealed class RedisSeatAvailabilityCache
{
    private readonly ConcurrentDictionary<string, SeatAvailability> _cache = new();

    public SeatAvailability? Get(Guid scheduleId, string travelClass, string quota)
    {
        return _cache.GetValueOrDefault(BuildKey(scheduleId, travelClass, quota));
    }

    public void Set(SeatAvailability availability)
    {
        _cache[BuildKey(availability.ScheduleId, availability.Class, availability.Quota)] = availability;
    }

    public void Invalidate(Guid scheduleId, string travelClass, string quota)
    {
        _cache.TryRemove(BuildKey(scheduleId, travelClass, quota), out _);
    }

    private static string BuildKey(Guid scheduleId, string travelClass, string quota) =>
        $"seat_availability:{scheduleId:N}:{travelClass.ToUpperInvariant()}:{quota.ToUpperInvariant()}";
}
