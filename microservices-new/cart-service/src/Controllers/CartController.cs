using System.Security.Claims;
using CartService.DTOs;
using CartService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly CartRedisService _cart;
    private readonly IHttpClientFactory _httpClientFactory;

    public CartController(CartRedisService cart, IHttpClientFactory httpClientFactory)
    {
        _cart = cart;
        _httpClientFactory = httpClientFactory;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var items = await _cart.GetCartAsync(GetUserId());
        return Ok(new CartDto(GetUserId(), items, items.Sum(i => i.Subtotal)));
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart(AddToCartDto dto)
    {
        // Check stock via Inventory Service
        var client = _httpClientFactory.CreateClient("inventory");
        var response = await client.GetAsync($"/api/inventory/{dto.ProductId}");
        if (!response.IsSuccessStatusCode) return NotFound("Product not found in inventory");

        var stock = await response.Content.ReadFromJsonAsync<int>();
        if (stock < dto.Quantity) return BadRequest("Insufficient stock");

        await _cart.AddItemAsync(GetUserId(), dto);
        return Ok();
    }

    [HttpPut("update/{productId}")]
    public async Task<IActionResult> UpdateItem(string productId, [FromBody] int quantity)
    {
        var updated = await _cart.UpdateItemAsync(GetUserId(), productId, quantity);
        return updated ? Ok() : NotFound();
    }

    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> RemoveItem(string productId)
    {
        var removed = await _cart.RemoveItemAsync(GetUserId(), productId);
        return removed ? Ok() : NotFound();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        await _cart.ClearAsync(GetUserId());
        return Ok();
    }
}
