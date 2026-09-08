namespace HRM.Shared.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    /// <summary>One topic exchange for the whole platform; routing key = event name.</summary>
    public string Exchange { get; set; } = "hrm.events";
}
