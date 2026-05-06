using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OrderProcessingService.Api.Domain;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}
