using Railway.Application;
using Railway.Application.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayCatalogServices();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { service = "catalog", status = "ok" }));
app.MapGet("/api/trains/search", async (
    string source,
    string destination,
    DateOnly date,
    TrainScheduleService trainSchedule) =>
    Results.Ok(await trainSchedule.SearchAsync(source, destination, date)));

app.Run();
