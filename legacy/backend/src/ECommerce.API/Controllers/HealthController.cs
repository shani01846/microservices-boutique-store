using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
        catch
        {
            return StatusCode(503, new { Status = "Unhealthy", Timestamp = DateTime.UtcNow });
        }
    }
}