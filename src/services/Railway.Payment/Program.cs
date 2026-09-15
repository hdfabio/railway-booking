using Railway.Application;
using Railway.Messaging;
using Railway.Payment;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayPaymentServices();
builder.Services.AddRabbitMqConsumer(builder.Configuration);
builder.Services.AddScoped<IEventHandler, PaymentEventHandler>();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { service = "payment", status = "ok" }));
app.Run();
