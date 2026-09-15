using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Railway.Contracts;

namespace Railway.Messaging;

public sealed class RabbitMqConsumerHost(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerHost> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

                var queueName = _channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true).QueueName;
                _channel.QueueBind(queueName, _options.ExchangeName, routingKey: "#");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (_, args) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(args.Body.ToArray());
                        var domainEvent = EventEnvelopeMapper.ToDomainEvent(json);

                        await using var scope = scopeFactory.CreateAsyncScope();
                        var handlers = scope.ServiceProvider.GetServices<IEventHandler>();
                        foreach (var handler in handlers.Where(h => h.CanHandle(domainEvent.Type)))
                        {
                            await handler.HandleAsync(domainEvent, stoppingToken);
                        }

                        _channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error handling RabbitMQ message");
                        _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel.BasicConsume(queueName, autoAck: false, consumer);
                logger.LogInformation("RabbitMQ consumer listening on {Exchange}", _options.ExchangeName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RabbitMQ consumer connection failed; retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
