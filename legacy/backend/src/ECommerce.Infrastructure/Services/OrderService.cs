using Microsoft.EntityFrameworkCore;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId, bool isAdmin)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!isAdmin)
            query = query.Where(o => o.UserId == userId);

        return await query.Select(o => ToDto(o)).ToListAsync();
    }

    public async Task<OrderDto> PlaceOrderAsync(int userId, CreateOrderDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = new Order { UserId = userId, Status = "Pending" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        decimal total = 0;

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"Product {item.ProductId} not found");

            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {product.Name}");

            product.StockQuantity -= item.Quantity;

            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * item.Quantity;
        }

        order.TotalAmount = total;
        order.Status = "Confirmed";

        // Clear cart
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
            _context.CartItems.RemoveRange(cart.CartItems);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetOrderDtoAsync(order.Id);
    }

    public async Task<OrderDto> PlaceOrderFromCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Cart is empty");

        var dto = new CreateOrderDto(
            cart.CartItems.Select(ci => new CreateOrderItemDto(ci.ProductId, ci.Quantity)).ToList()
        );

        return await PlaceOrderAsync(userId, dto);
    }

    private async Task<OrderDto> GetOrderDtoAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstAsync(o => o.Id == orderId);

        return ToDto(order);
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id,
        o.OrderDate,
        o.TotalAmount,
        o.Status,
        o.OrderItems.Select(oi => new OrderItemDto(
            oi.Id, oi.ProductId, oi.Product.Name, oi.Quantity, oi.UnitPrice
        )).ToList()
    );
}
