using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Domain.Entities;
using ECommerce.Application.DTOs;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();
            
        if (!isAdmin)
            query = query.Where(o => o.UserId == userId);

        var orders = await query
            .Select(o => new OrderDto(
                o.Id,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.OrderItems.Select(oi => new OrderItemDto(
                    oi.Id,
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Quantity,
                    oi.UnitPrice
                )).ToList()
            ))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrder)
    {
        var userId = GetUserId();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var order = new Order { UserId = userId, Status = "Processing" };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var item in createOrder.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    return BadRequest($"Product {item.ProductId} not found");

                if (product.StockQuantity < item.Quantity)
                    return BadRequest($"Insufficient stock for product {product.Name}");

                product.StockQuantity -= item.Quantity;

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                totalAmount += orderItem.UnitPrice * orderItem.Quantity;
                _context.OrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;
            order.Status = "Completed";
            
            // Clear user's cart
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var orderDto = new OrderDto(
                order.Id,
                order.OrderDate,
                order.TotalAmount,
                order.Status,
                new List<OrderItemDto>()
            );

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, orderDto);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Error processing order");
        }
    }

    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderDto>> CreateOrderFromCart()
    {
        var userId = GetUserId();
        
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
            
        if (cart == null || !cart.CartItems.Any())
            return BadRequest("Cart is empty");
            
        var orderItems = cart.CartItems.Select(ci => new CreateOrderItemDto(ci.ProductId, ci.Quantity)).ToList();
        var createOrderDto = new CreateOrderDto(orderItems);
        
        return await CreateOrder(createOrderDto);
    }
}