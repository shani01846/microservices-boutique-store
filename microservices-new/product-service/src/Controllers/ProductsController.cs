using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductRepository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ProductCacheService _cache;
    private readonly IWebHostEnvironment _env;

    public ProductsController(ProductRepository repo, IHttpClientFactory httpFactory, ProductCacheService cache, IWebHostEnvironment env)
    {
        _repo = repo;
        _httpFactory = httpFactory;
        _cache = cache;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var cached = await _cache.GetAllAsync();
        var products = cached ?? await _repo.GetAllAsync();
        if (cached == null) await _cache.SetAllAsync(products);

        var inventoryClient = _httpFactory.CreateClient("inventory");
        var stockTasks = products.Select(async p =>
        {
            try
            {
                var res = await inventoryClient.GetAsync($"/api/inventory/{p.Id}");
                var stock = res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<int>() : 0;
                return ToDto(p, stock);
            }
            catch { return ToDto(p, 0); }
        });

        return Ok(await Task.WhenAll(stockTasks));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(string id)
    {
        var cached = await _cache.GetAsync(id);
        var product = cached ?? await _repo.GetByIdAsync(id);
        if (product == null) return NotFound();
        if (cached == null) await _cache.SetAsync(product);

        var inventoryClient = _httpFactory.CreateClient("inventory");
        int stock = 0;
        try
        {
            var res = await inventoryClient.GetAsync($"/api/inventory/{id}");
            if (res.IsSuccessStatusCode) stock = await res.Content.ReadFromJsonAsync<int>();
        }
        catch { }

        return Ok(ToDto(product, stock));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Category = dto.Category,
            Size = dto.Size
        };

        await _repo.CreateAsync(product);
        await _cache.SetAsync(product);

        var client = _httpFactory.CreateClient("inventory");
        await client.PutAsJsonAsync($"/api/inventory/{product.Id}", dto.StockQuantity);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product, dto.StockQuantity));
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UploadImage(string id, IFormFile file)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return NotFound();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest("Only jpg, png, webp allowed.");

        var imagesDir = Path.Combine(_env.ContentRootPath, "images");
        Directory.CreateDirectory(imagesDir);

        var fileName = $"{id}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(imagesDir, fileName));
        await file.CopyToAsync(stream);

        product.ImageUrl = $"/images/{fileName}";
        product.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(id, product);
        await _cache.InvalidateAsync(id);

        return Ok(ToDto(product, 0));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, UpdateProductDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Price = dto.Price;
        existing.Category = dto.Category;
        existing.Size = dto.Size;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, existing);
        await _cache.InvalidateAsync(id);

        var client = _httpFactory.CreateClient("inventory");
        await client.PutAsJsonAsync($"/api/inventory/{id}", dto.StockQuantity);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _repo.DeleteAsync(id);
        await _cache.InvalidateAsync(id);

        var client = _httpFactory.CreateClient("inventory");
        await client.DeleteAsync($"/api/inventory/{id}");

        return NoContent();
    }

    private static ProductDto ToDto(Product p, int stock) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Category, p.Size, p.ImageUrl, stock);
}
