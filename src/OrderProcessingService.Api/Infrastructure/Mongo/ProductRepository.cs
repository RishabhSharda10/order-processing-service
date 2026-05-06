using MongoDB.Driver;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Infrastructure.Mongo;

public class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _collection;

    public ProductRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Product>("products");
    }

    public async Task<Product?> GetByIdAsync(string productId, CancellationToken cancellationToken)
    {
        Product? product = await _collection.Find(p => p.Id == productId).FirstOrDefaultAsync(cancellationToken);
        return product;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await _collection.Find(FilterDefinition<Product>.Empty).ToListAsync(cancellationToken);
        return list;
    }

    public async Task<bool> TryDecrementStockAsync(string productId, int quantity, CancellationToken cancellationToken)
    {
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            Builders<Product>.Filter.Gte(p => p.StockQuantity, quantity));

        var update = Builders<Product>.Update.Inc(p => p.StockQuantity, -quantity);

        var options = new FindOneAndUpdateOptions<Product>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = false
        };

        var updated = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return updated is not null;
    }

    public Task IncrementStockAsync(string productId, int quantity, CancellationToken cancellationToken)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Id, productId);
        var update = Builders<Product>.Update.Inc(p => p.StockQuantity, quantity);
        return _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public Task<long> CountAsync(CancellationToken cancellationToken) =>
        _collection.CountDocumentsAsync(FilterDefinition<Product>.Empty, cancellationToken: cancellationToken);

    public Task InsertManyAsync(IEnumerable<Product> products, CancellationToken cancellationToken) =>
        _collection.InsertManyAsync(products, cancellationToken: cancellationToken);
}
