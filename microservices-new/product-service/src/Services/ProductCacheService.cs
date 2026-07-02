using System.Text.Json;
using ProductService.Models;
using StackExchange.Redis;

namespace ProductService.Services;

public class ProductCacheService
{
    private readonly IDatabase _db;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string AllKey = "products:all";

    public ProductCacheService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    private static string Key(string id) => $"product:{id}";

    public async Task<Product?> GetAsync(string id)
    {
        var val = await _db.StringGetAsync(Key(id));
        if (val.HasValue)
        {
            Console.WriteLine($"[Cache] HIT product:{id}");
            return JsonSerializer.Deserialize<Product>(val!);
        }
        Console.WriteLine($"[Cache] MISS product:{id}");
        return null;
    }

    public async Task<List<Product>?> GetAllAsync()
    {
        var val = await _db.StringGetAsync(AllKey);
        if (val.HasValue)
        {
            Console.WriteLine("[Cache] HIT products:all");
            return JsonSerializer.Deserialize<List<Product>>(val!);
        }
        Console.WriteLine("[Cache] MISS products:all");
        return null;
    }

    public async Task SetAsync(Product product)
    {
        await _db.StringSetAsync(Key(product.Id), JsonSerializer.Serialize(product), Ttl);
        await _db.KeyDeleteAsync(AllKey);
    }

    public async Task SetAllAsync(List<Product> products) =>
        await _db.StringSetAsync(AllKey, JsonSerializer.Serialize(products), Ttl);

    public async Task InvalidateAsync(string id)
    {
        Console.WriteLine($"[Cache] INVALIDATE product:{id}");
        await _db.KeyDeleteAsync(Key(id));
        await _db.KeyDeleteAsync(AllKey);
    }
}
