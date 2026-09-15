using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Railway.Contracts;

namespace Railway.Messaging;

public sealed class RabbitMqEventBus(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqEventBus> logger) : IEventBus, IDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _sync = new();

    public Task PublishAsync(string eventType, string aggregateId, object payload, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(EventEnvelopeMapper.Serialize(eventType, aggregateId, payload));

        try
        {
            EnsureChannel();
            var properties = _channel!.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;
            properties.Type = eventType;

            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: eventType,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish {EventType} to RabbitMQ", eventType);
            throw;
        }

        return Task.CompletedTask;
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        lock (_sync)
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };

            _connection?.Dispose();
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            logger.LogInformation("RabbitMQ publisher connected to {Exchange}", _options.ExchangeName);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
