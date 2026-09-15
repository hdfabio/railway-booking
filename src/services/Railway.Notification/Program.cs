using Railway.Application;
using Railway.Messaging;
using Railway.Notification;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayNotificationServices();
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddRabbitMqConsumer(builder.Configuration);
builder.Services.AddScoped<IEventHandler, NotificationEventHandler>();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { service = "notification", status = "ok" }));
app.Run();
