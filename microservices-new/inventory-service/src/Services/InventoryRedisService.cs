using StackExchange.Redis;

namespace InventoryService.Services;

public class InventoryRedisService
{
    private readonly IDatabase _db;

    public InventoryRedisService(IConnectionMultiplexer redis) =>
        _db = redis.GetDatabase();

    private static string Key(string productId) => $"inventory:{productId}";

    public async Task<int?> GetStockAsync(string productId)
    {
        var val = await _db.StringGetAsync(Key(productId));
        return val.HasValue ? (int)val : null;
    }

    public async Task SetStockAsync(string productId, int quantity) =>
        await _db.StringSetAsync(Key(productId), quantity);

    public async Task<bool> ReserveAsync(string productId, int quantity)
    {
        var tran = _db.CreateTransaction();
        var current = (int)await _db.StringGetAsync(Key(productId));
        if (current < quantity) return false;
        tran.AddCondition(Condition.StringEqual(Key(productId), current));
        _ = tran.StringDecrementAsync(Key(productId), quantity);
        return await tran.ExecuteAsync();
    }

    public async Task ReleaseAsync(string productId, int quantity) =>
        await _db.StringIncrementAsync(Key(productId), quantity);

    public async Task DeleteAsync(string productId) =>
        await _db.KeyDeleteAsync(Key(productId));
}
