using MongoDB.Bson;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Infrastructure.Seeding;

public class ProductSeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProductSeedHostedService> _logger;

    public ProductSeedHostedService(IServiceProvider services, ILogger<ProductSeedHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        if (await repo.CountAsync(cancellationToken) > 0)
            return;

        var products = new List<Product>
        {
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with USB receiver.",
                Price = 29.99m,
                StockQuantity = 120
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Mechanical Keyboard",
                Description = "Tenkeyless RGB mechanical keyboard.",
                Price = 119.50m,
                StockQuantity = 45
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "USB-C Hub",
                Description = "7-in-1 USB-C hub with HDMI and SD reader.",
                Price = 49.00m,
                StockQuantity = 200
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Noise-Cancelling Headphones",
                Description = "Over-ear ANC headphones, 30h battery.",
                Price = 199.00m,
                StockQuantity = 30
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "1080p Webcam",
                Description = "Autofocus webcam with dual microphones.",
                Price = 79.25m,
                StockQuantity = 75
            }
        };

        await repo.InsertManyAsync(products, cancellationToken);
        _logger.LogInformation("Seeded {Count} products.", products.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
