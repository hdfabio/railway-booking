using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Railway.Application;
using Railway.Application.Auth;
using Railway.Application.EventHandlers;
using Railway.Application.Services;
using Railway.Contracts;
using Railway.Messaging;
using Railway.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayAuth();
builder.Services.AddRailwayJwtAuthentication(builder.Configuration);
builder.Services.AddRailwayCatalogServices();
builder.Services.AddRailwayInventoryServices();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddRailwayNotificationServices();
builder.Services.AddScoped<ReportingService>();

builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
builder.Services.AddScoped<IEventHandler, SeatInventoryEventHandler>();
builder.Services.AddScoped<IEventHandler, PaymentEventHandler>();
builder.Services.AddScoped<IEventHandler, NotificationEventHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
    options.AddPolicy("api", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

var app = builder.Build();
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
await DatabaseInitializer.InitializeAsync(app.Services);

app.UseCors("AngularDev");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/api/health")).AllowAnonymous();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    architecture = "modular-monolith",
    events = "in-process",
    clientContract = "rest",
    modules = new[] { "Identity", "Catalog", "Inventory", "Booking", "Payment", "Notification", "Reporting", "Events" }
})).RequireRateLimiting("api").AllowAnonymous();

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    var token = await auth.LoginAsync(request);
    return token is null ? Results.Unauthorized() : Results.Ok(token);
}).RequireRateLimiting("api").AllowAnonymous();

app.MapGet("/api/users/me", async (UserService users) =>
{
    var profile = await users.GetCurrentUserAsync();
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
}).RequireAuthorization().RequireRateLimiting("api");

app.MapGet("/api/trains/search", async (
    string source,
    string destination,
    DateOnly date,
    string @class,
    string quota,
    TrainScheduleService trainSchedule,
    ISeatInventoryService seatInventory) =>
{
    var trains = await trainSchedule.SearchAsync(source, destination, date);
    if (trains.Count == 0)
    {
        return Results.Ok(trains);
    }

    var availabilityBySchedule = await seatInventory.GetAvailabilityForSchedulesAsync(
        trains.Select(train => train.ScheduleId).ToArray(),
        @class,
        quota);

    return Results.Ok(trains.Select(train => train with
    {
        Availability = availabilityBySchedule.GetValueOrDefault(train.ScheduleId)
    }).ToArray());
}).RequireRateLimiting("api").AllowAnonymous();

app.MapGet("/api/schedules/{scheduleId:guid}/availability", async (
    Guid scheduleId,
    string @class,
    string quota,
    ISeatInventoryService seatInventory) =>
    Results.Ok(await seatInventory.GetAvailabilityAsync(scheduleId, @class, quota)))
    .RequireRateLimiting("api")
    .AllowAnonymous();

app.MapPost("/api/bookings", async (CreateBookingRequest request, BookingService bookingService) =>
{
    var result = await bookingService.CreateAsync(request);
    return result.Success
        ? Results.Created($"/api/bookings/{result.Booking!.Pnr}", result.Booking)
        : Results.BadRequest(result);
}).RequireAuthorization().RequireRateLimiting("api");

app.MapGet("/api/bookings", async (BookingService bookingService) =>
    Results.Ok(await bookingService.GetForCurrentUserAsync()))
    .RequireAuthorization()
    .RequireRateLimiting("api");

app.MapGet("/api/users/{userId:guid}/bookings", async (
    Guid userId,
    BookingService bookingService,
    ICurrentUserService currentUser) =>
{
    if (!currentUser.IsAuthenticated || currentUser.UserId != userId)
    {
        return Results.Forbid();
    }

    return Results.Ok(await bookingService.GetForUserAsync(userId));
}).RequireAuthorization().RequireRateLimiting("api");

app.MapGet("/api/bookings/{pnr}", async (string pnr, BookingService bookings) =>
{
    var booking = await bookings.GetByPnrAsync(pnr);
    return booking is null ? Results.NotFound() : Results.Ok(booking);
}).RequireRateLimiting("api").AllowAnonymous();

app.MapPost("/api/bookings/{pnr}/cancel", async (string pnr, BookingService bookingService) =>
{
    var result = await bookingService.CancelAsync(pnr);
    return result.Success ? Results.Ok(result.Booking) : Results.BadRequest(result);
}).RequireAuthorization().RequireRateLimiting("api");

app.MapGet("/api/reports/summary", (ReportingService reporting) => reporting.GetSummaryAsync())
    .RequireAuthorization()
    .RequireRateLimiting("api");

app.MapGet("/api/events", (IEventStore events) => events.GetRecent())
    .RequireAuthorization()
    .RequireRateLimiting("api");

app.Run();
