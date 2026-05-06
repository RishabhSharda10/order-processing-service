using System.Text.Json.Serialization;
using MongoDB.Driver;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Infrastructure.Messaging;
using OrderProcessingService.Api.Infrastructure.Mongo;
using OrderProcessingService.Api.Infrastructure.Redis;
using OrderProcessingService.Api.Infrastructure.Seeding;
using OrderProcessingService.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));

var mongoSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>()
                    ?? throw new InvalidOperationException("MongoDb settings are required.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoSettings.DatabaseName);
});

var redisSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()
                    ?? new RedisSettings();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var cfg = ConfigurationOptions.Parse(redisSettings.ConnectionString);
    cfg.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(cfg);
});

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<IOrderEventPublisher, RabbitMqOrderEventPublisher>();

builder.Services.AddSingleton<IProductReadService, ProductReadService>();
builder.Services.AddSingleton<IOrderService, OrderService>();

builder.Services.AddHostedService<ProductSeedHostedService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

app.Run();
