using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderBusinessService _orderService;
    private readonly IHttpClientFactory _httpFactory;

    public OrdersController(OrderBusinessService orderService, IHttpClientFactory httpFactory)
    {
        _orderService = orderService;
        _httpFactory = httpFactory;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetEmail() =>
        User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    private string GetName() =>
        User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

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
            var order = await _orderService.PlaceOrderAsync(GetUserId(), GetEmail(), GetName(), dto);
            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("from-cart")]
    public async Task<ActionResult<OrderDto>> CreateOrderFromCart()
    {
        try
        {
            var cartClient = _httpFactory.CreateClient("cart");
            var token = Request.Headers["Authorization"].ToString();
            cartClient.DefaultRequestHeaders.Add("Authorization", token);

            var cartResponse = await cartClient.GetAsync("/api/cart");
            if (!cartResponse.IsSuccessStatusCode)
                return BadRequest("Could not retrieve cart");

            var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponse>();
            if (cart == null || cart.Items.Count == 0)
                return BadRequest("Cart is empty");

            var order = await _orderService.PlaceOrderFromCartAsync(GetUserId(), GetEmail(), GetName(), cart.Items);
            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}

