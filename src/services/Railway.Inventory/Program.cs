using Railway.Application;
using Railway.Application.Services;
using Railway.Contracts;
using Railway.Inventory;
using Railway.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayInventoryServices();
builder.Services.AddRabbitMqConsumer(builder.Configuration);
builder.Services.AddScoped<IEventHandler, SeatInventoryEventHandler>();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { service = "inventory", status = "ok" }));
app.MapGet("/api/schedules/{scheduleId:guid}/availability", async (
    Guid scheduleId,
    string @class,
    string quota,
    ISeatInventoryService seatInventory) =>
    Results.Ok(await seatInventory.GetAvailabilityAsync(scheduleId, @class, quota)));

app.MapPost("/api/availability/batch", async (
    Guid[] scheduleIds,
    string @class,
    string quota,
    ISeatInventoryService seatInventory) =>
{
    if (scheduleIds.Length == 0)
    {
        return Results.Ok(new Dictionary<Guid, SeatAvailability>());
    }

    return Results.Ok(await seatInventory.GetAvailabilityForSchedulesAsync(scheduleIds, @class, quota));
});

app.Run();
