using OrderProcessingService.Api.Contracts;

namespace OrderProcessingService.Api.Abstractions;

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken);
}
