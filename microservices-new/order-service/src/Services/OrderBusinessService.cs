using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderBusinessService> _logger;

    public OrderBusinessService(
        OrderDbContext context,
        IConnection rabbit,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderBusinessService> logger)
    {
        _context = context;
        _rabbit = rabbit;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(string userId, bool isAdmin)
    {
        var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();
        if (!isAdmin) query = query.Where(o => o.UserId == userId);
        return await query.Select(o => ToDto(o)).ToListAsync();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string userId, bool isAdmin)
    {
        var query = _context.Orders.Include(o => o.OrderItems).Where(o => o.Id == orderId).AsQueryable();
        if (!isAdmin) query = query.Where(o => o.UserId == userId);
        var order = await query.FirstOrDefaultAsync();
        return order == null ? null : ToDto(order);
    }

    public async Task<OrderDto> PlaceOrderAsync(string userId, string customerEmail, string customerName, CreateOrderDto dto)
    {
        _logger.LogInformation("Rabbit flow start: creating order for UserId={UserId} with {ItemCount} items", userId, dto.Items.Count);

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

        _logger.LogInformation("Rabbit flow step complete: order.placed published for OrderId={OrderId}", order.Id);

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
        _logger.LogInformation("RabbitMQ exchange declared: Exchange={Exchange}", "order.placed");

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
            basicProperties: GetProperties(channel),
            body: Encoding.UTF8.GetBytes(payload));

        _logger.LogInformation("RabbitMQ published: Exchange={Exchange}, Event=order.placed, OrderId={OrderId}", "order.placed", orderId);
    }

    private IBasicProperties GetProperties(IModel channel)
    {
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        // Attach correlation id from the current HTTP context if available
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var corr = context?.Request?.Headers["X-Correlation-ID"].ToString();
            if (!string.IsNullOrEmpty(corr))
            {
                props.Headers = props.Headers ?? new Dictionary<string, object>();
                props.Headers["X-Correlation-ID"] = Encoding.UTF8.GetBytes(corr);
                _logger.LogInformation("RabbitMQ header attached: X-Correlation-ID={CorrelationId}", corr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed attaching RabbitMQ correlation header");
        }
        return props;
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
