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
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var cartItems = cart.CartItems.Select(ci => new CartItemDto(
            ci.Id,
            ci.ProductId,
            ci.Product.Name,
            ci.Product.Price,
            ci.Quantity,
            ci.Product.Price * ci.Quantity
        )).ToList();

        var total = cartItems.Sum(ci => ci.Subtotal);

        return Ok(new CartDto(cart.Id, cart.UserId, cartItems, total));
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddToCart(AddToCartDto addToCartDto)
    {
        var userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var product = await _context.Products.FindAsync(addToCartDto.ProductId);
        if (product == null)
            return NotFound("Product not found");

        if (product.StockQuantity < addToCartDto.Quantity)
            return BadRequest("Insufficient stock");

        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addToCartDto.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += addToCartDto.Quantity;
            existingItem.Cart.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = addToCartDto.ProductId,
                Quantity = addToCartDto.Quantity
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("update/{itemId}")]
    public async Task<ActionResult> UpdateCartItem(int itemId, [FromBody] int quantity)
    {
        var userId = GetUserId();
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

        if (cartItem == null)
            return NotFound();

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            if (cartItem.Product.StockQuantity < quantity)
                return BadRequest("Insufficient stock");
            
            cartItem.Quantity = quantity;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("remove/{itemId}")]
    public async Task<ActionResult> RemoveFromCart(int itemId)
    {
        var userId = GetUserId();
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

        if (cartItem == null)
            return NotFound();

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}