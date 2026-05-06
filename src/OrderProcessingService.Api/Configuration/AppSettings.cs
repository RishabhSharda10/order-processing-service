namespace OrderProcessingService.Api.Configuration;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "order_processing";
}

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>Cache TTL for product list and single product reads.</summary>
    public int ProductCacheTtlSeconds { get; set; } = 60;

    /// <summary>Cache TTL for order reads.</summary>
    public int OrderCacheTtlSeconds { get; set; } = 30;
}

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string ExchangeName { get; set; } = "orders.events";

    public string OrderCreatedRoutingKey { get; set; } = "order.created";
}
