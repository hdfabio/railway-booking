using Railway.Application;
using Railway.Application.Auth;
using Railway.Application.Services;
using Railway.Contracts;
using Railway.Messaging;
using Railway.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayBookingServices();
builder.Services.AddRailwayJwtAuthentication(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("AngularDev", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
await DatabaseInitializer.InitializeAsync(app.Services);
app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { service = "booking", status = "ok" }));
app.MapPost("/api/bookings", async (CreateBookingRequest request, BookingService bookingService) =>
{
    var result = await bookingService.CreateAsync(request);
    return result.Success ? Results.Created($"/api/bookings/{result.Booking!.Pnr}", result.Booking) : Results.BadRequest(result);
}).RequireAuthorization();
app.MapGet("/api/bookings", async (BookingService bookingService) =>
    Results.Ok(await bookingService.GetForCurrentUserAsync())).RequireAuthorization();
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
}).RequireAuthorization();
app.MapGet("/api/bookings/{pnr}", async (string pnr, BookingService bookings) =>
{
    var booking = await bookings.GetByPnrAsync(pnr);
    return booking is null ? Results.NotFound() : Results.Ok(booking);
}).AllowAnonymous();
app.MapPost("/api/bookings/{pnr}/cancel", async (string pnr, BookingService bookingService) =>
{
    var result = await bookingService.CancelAsync(pnr);
    return result.Success ? Results.Ok(result.Booking) : Results.BadRequest(result);
}).RequireAuthorization();

app.Run();
