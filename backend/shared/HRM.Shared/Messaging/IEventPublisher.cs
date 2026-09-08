using HRM.Shared.Contracts;

namespace HRM.Shared.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent;
}
