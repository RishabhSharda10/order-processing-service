using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Contracts;
using RabbitMQ.Client;

namespace OrderProcessingService.Api.Infrastructure.Messaging;

public sealed class RabbitMqOrderEventPublisher : IOrderEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _gate = new();

    public RabbitMqOrderEventPublisher(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    public Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken)
    {
        EnsureInfrastructure();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt, _jsonOptions));
        var props = _channel!.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.OrderCreatedRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }

    private void EnsureInfrastructure()
    {
        lock (_gate)
        {
            if (_channel is { IsOpen: true })
                return;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = string.IsNullOrEmpty(_settings.VirtualHost) ? "/" : _settings.VirtualHost
            };

            _connection?.Dispose();
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Topic, durable: true);
        }
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
