using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Services;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryRedisService _inventory;

    public InventoryController(InventoryRedisService inventory) => _inventory = inventory;

    [HttpGet("{productId}")]
    public async Task<ActionResult<int>> GetStock(string productId)
    {
        var stock = await _inventory.GetStockAsync(productId);
        return stock == null ? NotFound($"Product {productId} not found in inventory") : Ok(stock);
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> SetStock(string productId, [FromBody] int quantity)
    {
        if (quantity < 0) return BadRequest("Quantity cannot be negative");
        await _inventory.SetStockAsync(productId, quantity);
        return NoContent();
    }

    [HttpPost("{productId}/reserve")]
    [Authorize]
    public async Task<IActionResult> Reserve(string productId, [FromBody] int quantity)
    {
        var success = await _inventory.ReserveAsync(productId, quantity);
        return success ? Ok() : BadRequest("Insufficient stock");
    }

    [HttpPost("{productId}/release")]
    [Authorize]
    public async Task<IActionResult> Release(string productId, [FromBody] int quantity)
    {
        await _inventory.ReleaseAsync(productId, quantity);
        return Ok();
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Delete(string productId)
    {
        await _inventory.DeleteAsync(productId);
        return NoContent();
    }
}
