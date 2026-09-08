using System.Text;
using System.Text.Json;
using HRM.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HRM.Shared.Messaging;

/// <summary>
/// Publishes integration events to a durable topic exchange.
/// Connection is created lazily and reused - opening a connection per message is expensive.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly Lazy<IConnection> _connection;

    public RabbitMqEventPublisher(IOptions<RabbitMqSettings> options, ILogger<RabbitMqEventPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _connection = new Lazy<IConnection>(() =>
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };
            return factory.CreateConnection($"publisher-{Environment.MachineName}");
        });
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        try
        {
            using var channel = _connection.Value.CreateModel();
            channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, @event.GetType()));
            var props = channel.CreateBasicProperties();
            props.Persistent = true;                  // survives a broker restart
            props.MessageId = @event.EventId.ToString();
            props.Type = @event.EventName;
            props.CorrelationId = @event.CorrelationId;
            props.ContentType = "application/json";

            channel.BasicPublish(_settings.Exchange, @event.EventName, props, body);
            _logger.LogInformation("Published {EventName} {EventId}", @event.EventName, @event.EventId);
        }
        catch (Exception ex)
        {
            // Publishing must never break the HTTP request that triggered it.
            // In a later phase this is replaced by the Outbox pattern for guaranteed delivery.
            _logger.LogError(ex, "Failed to publish {EventName}", @event.EventName);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated) _connection.Value.Dispose();
    }
}
