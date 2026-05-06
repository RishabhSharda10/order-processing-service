using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken);

    Task InsertAsync(Order order, CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(Order order, CancellationToken cancellationToken);
}
