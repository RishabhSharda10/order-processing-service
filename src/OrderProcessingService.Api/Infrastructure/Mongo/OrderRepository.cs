using MongoDB.Driver;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Infrastructure.Mongo;

public class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _collection;

    public OrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Order>("orders");
    }

    public async Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken)
    {
        Order? order = await _collection.Find(o => o.Id == orderId).FirstOrDefaultAsync(cancellationToken);
        return order;
    }

    public Task InsertAsync(Order order, CancellationToken cancellationToken) =>
        _collection.InsertOneAsync(order, cancellationToken: cancellationToken);

    public async Task<bool> ReplaceAsync(Order order, CancellationToken cancellationToken)
    {
        var result = await _collection.ReplaceOneAsync(
            o => o.Id == order.Id,
            order,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.ModifiedCount > 0;
    }
}
