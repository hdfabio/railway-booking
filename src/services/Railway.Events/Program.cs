using Railway.Application;
using Railway.Events;
using Railway.Messaging;
using Railway.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<EventStreamStore>();
builder.Services.AddRailwayJwtAuthentication(builder.Configuration);
builder.Services.AddRabbitMqConsumer(builder.Configuration);
builder.Services.AddScoped<IEventHandler, EventStreamEventHandler>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/health", () => Results.Ok(new { service = "events", status = "ok" }));
app.MapGet("/api/events", (EventStreamStore store) => store.GetRecent()).RequireAuthorization();
app.Run();
