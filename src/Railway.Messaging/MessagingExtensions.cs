using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Railway.Contracts;

namespace Railway.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddHostedService<RabbitMqConsumerHost>();
        return services;
    }
}
