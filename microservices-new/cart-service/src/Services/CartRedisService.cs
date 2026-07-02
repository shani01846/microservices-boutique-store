using System.Text.Json;
using CartService.DTOs;
using StackExchange.Redis;

namespace CartService.Services;

public class CartRedisService
{
    private readonly IDatabase _db;

    public CartRedisService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    private static string Key(string userId) => $"cart:{userId}";

    public async Task<List<CartItemDto>> GetCartAsync(string userId)
    {
        var data = await _db.StringGetAsync(Key(userId));
        return data.HasValue
            ? JsonSerializer.Deserialize<List<CartItemDto>>(data!) ?? []
            : [];
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        var items = await GetCartAsync(userId);
        var existing = items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existing != null)
        {
            items.Remove(existing);
            items.Add(existing with { Quantity = existing.Quantity + dto.Quantity, Subtotal = existing.Price * (existing.Quantity + dto.Quantity) });
        }
        else
        {
            items.Add(new CartItemDto(dto.ProductId, dto.ProductName, dto.Price, dto.Quantity, dto.Price * dto.Quantity));
        }

        await SaveAsync(userId, items);
    }

    public async Task<bool> UpdateItemAsync(string userId, string productId, int quantity)
    {
        var items = await GetCartAsync(userId);
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return false;

        items.Remove(item);
        if (quantity > 0)
            items.Add(item with { Quantity = quantity, Subtotal = item.Price * quantity });

        await SaveAsync(userId, items);
        return true;
    }

    public async Task<bool> RemoveItemAsync(string userId, string productId)
    {
        var items = await GetCartAsync(userId);
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return false;
        items.Remove(item);
        await SaveAsync(userId, items);
        return true;
    }

    public async Task ClearAsync(string userId) =>
        await _db.KeyDeleteAsync(Key(userId));

    private async Task SaveAsync(string userId, List<CartItemDto> items) =>
        await _db.StringSetAsync(Key(userId), JsonSerializer.Serialize(items));
}
