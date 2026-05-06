namespace OrderProcessingService.Api.Services;

public static class CacheKeys
{
    public const string ProductsAll = "ops:products:all";

    public static string Product(string productId) => $"ops:product:{productId}";

    public static string Order(string orderId) => $"ops:order:{orderId}";
}
