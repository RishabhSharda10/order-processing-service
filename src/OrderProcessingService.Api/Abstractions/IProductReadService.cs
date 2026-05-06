using OrderProcessingService.Api.Contracts;

namespace OrderProcessingService.Api.Abstractions;

public interface IProductReadService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken);
}
