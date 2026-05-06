using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OrderProcessingService.Api.Domain;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public IReadOnlyList<OrderLineItem> Items { get; set; } = Array.Empty<OrderLineItem>();

    public OrderStatus Status { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class OrderLineItem
{
    public string ProductId { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }
}
