using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(string productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Atomically decreases stock when sufficient quantity exists.</summary>
    Task<bool> TryDecrementStockAsync(string productId, int quantity, CancellationToken cancellationToken);

    Task IncrementStockAsync(string productId, int quantity, CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);

    Task InsertManyAsync(IEnumerable<Product> products, CancellationToken cancellationToken);
}
