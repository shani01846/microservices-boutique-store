using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Domain.Entities;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using BCrypt.Net;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthController(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterUserDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            return BadRequest("Email already exists");

        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Role = "Customer"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var cart = new Cart { UserId = user.Id };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role);
        
        return Ok(new AuthResponseDto(token, userDto));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginUserDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role);
        
        return Ok(new AuthResponseDto(token, userDto));
    }

    [HttpPost("admin-register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAdmin(RegisterUserDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            return BadRequest("Email already exists");

        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Role = "Admin"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role);
        
        return Ok(new AuthResponseDto(token, userDto));
    }
}