using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using RabbitMQ.Client;

namespace OrderService.Services;

public class OrderBusinessService
{
    private readonly OrderDbContext _context;
    private readonly IConnection _rabbit;

    public OrderBusinessService(OrderDbContext context, IConnection rabbit)
    {
        _context = context;
        _rabbit = rabbit;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(string userId, bool isAdmin)
    {
        var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();
        if (!isAdmin) query = query.Where(o => o.UserId == userId);
        return await query.Select(o => ToDto(o)).ToListAsync();
    }

    public async Task<OrderDto> PlaceOrderAsync(string userId, string customerEmail, string customerName, CreateOrderDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = new Order { UserId = userId, Status = "Pending" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        order.TotalAmount = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        PublishOrderPlaced(userId, customerEmail, customerName, order.Id, order.OrderDate, order.TotalAmount, dto.Items);

        return await GetOrderDtoAsync(order.Id);
    }

    public async Task<OrderDto> PlaceOrderFromCartAsync(string userId, string customerEmail, string customerName, List<CartItemResponse> cartItems)
    {
        var dto = new CreateOrderDto(
            cartItems.Select(i => new CreateOrderItemDto(i.ProductId, i.ProductName, i.Price, i.Quantity)).ToList()
        );
        return await PlaceOrderAsync(userId, customerEmail, customerName, dto);
    }

    private void PublishOrderPlaced(string userId, string customerEmail, string customerName, int orderId, DateTime orderDate, decimal totalAmount, IEnumerable<CreateOrderItemDto> items)
    {
        using var channel = _rabbit.CreateModel();
        channel.ExchangeDeclare("order.placed", ExchangeType.Fanout, durable: true);

        var payload = JsonSerializer.Serialize(new
        {
            OrderId = orderId,
            UserId = userId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            OrderDate = orderDate,
            TotalAmount = totalAmount,
            Items = items.Select(i => new { i.ProductId, i.Quantity })
        });

        channel.BasicPublish(
            exchange: "order.placed",
            routingKey: "",
            body: Encoding.UTF8.GetBytes(payload));
    }

    private async Task<OrderDto> GetOrderDtoAsync(int orderId)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == orderId);
        return ToDto(order);
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.OrderDate, o.TotalAmount, o.Status,
        o.OrderItems.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()
    );
}

public record CartResponse(string UserId, List<CartItemResponse> Items, decimal Total);
public record CartItemResponse(string ProductId, string ProductName, decimal Price, int Quantity, decimal Subtotal);
