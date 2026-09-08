using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HRM.Shared.Messaging;

/// <summary>
/// Base background consumer. Declares a durable queue, binds the routing keys a service
/// cares about, and dispatches each message to HandleAsync. Ack only after success.
/// </summary>
public abstract class RabbitMqConsumerBase(
    IOptions<RabbitMqSettings> options,
    ILogger logger) : BackgroundService
{
    protected readonly RabbitMqSettings Settings = options.Value;
    protected readonly ILogger Logger = logger;

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }
    protected abstract Task HandleAsync(string eventName, string json, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection? connection = null;
        // RabbitMQ may still be booting when this service starts - retry with backoff.
        for (var attempt = 1; attempt <= 20 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = Settings.Host, Port = Settings.Port,
                    UserName = Settings.UserName, Password = Settings.Password,
                    DispatchConsumersAsync = true, AutomaticRecoveryEnabled = true
                };
                connection = factory.CreateConnection($"consumer-{QueueName}");
                break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}). Retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
        if (connection is null) return;

        var channel = connection.CreateModel();
        channel.ExchangeDeclare(Settings.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        foreach (var key in RoutingKeys)
            channel.QueueBind(QueueName, Settings.Exchange, key);
        channel.BasicQos(0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var eventName = ea.BasicProperties.Type ?? ea.RoutingKey;
            try
            {
                await HandleAsync(eventName, json, stoppingToken);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed handling {EventName}", eventName);
                // requeue:false -> goes to dead-letter later; avoids an infinite poison-message loop
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer);
        Logger.LogInformation("Listening on queue {Queue} for {Keys}", QueueName, string.Join(", ", RoutingKeys));

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        channel.Dispose();
        connection.Dispose();
    }
}
