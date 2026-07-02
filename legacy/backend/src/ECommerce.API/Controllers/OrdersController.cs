using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _orderService.GetOrdersAsync(GetUserId(), User.IsInRole("Admin"));
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.PlaceOrderAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderDto>> CreateOrderFromCart()
    {
        try
        {
            var order = await _orderService.PlaceOrderFromCartAsync(GetUserId());
            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
