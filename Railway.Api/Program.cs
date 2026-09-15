var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "Use the Railway modular monolith API host at http://localhost:5000",
    docs = "See README.md — run scripts/run-api.ps1"
}));

app.Run();
