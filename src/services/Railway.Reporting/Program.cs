using Railway.Application;
using Railway.Application.Services;
using Railway.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayReportingServices();
builder.Services.AddRailwayJwtAuthentication(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/health", () => Results.Ok(new { service = "reporting", status = "ok" }));
app.MapGet("/api/reports/summary", (ReportingService reporting) => reporting.GetSummaryAsync())
    .RequireAuthorization();
app.Run();
