using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Domain.Entities;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InventoryController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Inventories.Include(i => i.Product).ToListAsync());

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        return inv is null ? NotFound() : Ok(inv);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStock([FromBody] SetStockDto dto)
    {
        var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);

        if (inv is null)
        {
            inv = new Inventory { ProductId = dto.ProductId, QuantityAvailable = dto.Quantity };
            _context.Inventories.Add(inv);
        }
        else
        {
            inv.QuantityAvailable = dto.Quantity;
        }

        await _context.SaveChangesAsync();
        return Ok(inv);
    }
}

public record SetStockDto(int ProductId, int Quantity);
