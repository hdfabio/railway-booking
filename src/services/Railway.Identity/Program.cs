using Railway.Application;
using Railway.Application.Auth;
using Railway.Application.Services;
using Railway.Contracts;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRailwayPersistence(builder.Configuration);
builder.Services.AddRailwayAuth();
builder.Services.AddRailwayJwtAuthentication(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("AngularDev", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { service = "identity", status = "ok" }));
app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    var token = await auth.LoginAsync(request);
    return token is null ? Results.Unauthorized() : Results.Ok(token);
}).AllowAnonymous();
app.MapGet("/api/users/me", async (UserService users) =>
{
    var profile = await users.GetCurrentUserAsync();
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
}).RequireAuthorization();

app.Run();
