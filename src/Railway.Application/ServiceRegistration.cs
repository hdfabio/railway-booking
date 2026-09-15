using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Railway.Application.Auth;
using Railway.Application.Services;
using Railway.Persistence;

namespace Railway.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddRailwayPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RailwayDatabase")
            ?? "Data Source=App_Data/railway.db";

        services.AddDbContext<RailwayDbContext>(options =>
        {
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    public static IServiceCollection AddRailwayAuth(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        return services;
    }

    public static IServiceCollection AddRailwayCatalogServices(this IServiceCollection services)
    {
        services.AddScoped<TrainScheduleService>();
        return services;
    }

    public static IServiceCollection AddRailwayInventoryServices(this IServiceCollection services)
    {
        services.AddSingleton<RedisSeatAvailabilityCache>();
        services.AddScoped<ISeatInventoryService, SeatInventoryService>();
        return services;
    }

    public static IServiceCollection AddRailwayBookingServices(this IServiceCollection services)
    {
        services.AddRailwayAuth();
        services.AddRailwayInventoryServices();
        services.AddScoped<PaymentService>();
        services.AddScoped<BookingService>();
        return services;
    }

    public static IServiceCollection AddRailwayPaymentServices(this IServiceCollection services)
    {
        services.AddScoped<PaymentService>();
        return services;
    }

    public static IServiceCollection AddRailwayNotificationServices(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        return services;
    }

    public static IServiceCollection AddRailwayReportingServices(this IServiceCollection services)
    {
        services.AddRailwayAuth();
        services.AddScoped<ReportingService>();
        return services;
    }
}
