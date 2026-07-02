using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly JwtService _jwt;

    public AuthController(UserDbContext context, JwtService jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        => await CreateUser(dto, "Customer");

    [HttpPost("admin-register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAdmin(RegisterDto dto)
        => await CreateUser(dto, "Admin");

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        return Ok(BuildResponse(user));
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }

    private async Task<ActionResult<AuthResponseDto>> CreateUser(RegisterDto dto, string role)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(BuildResponse(user));
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var token = _jwt.GenerateToken(user.Id, user.Email, user.Role, $"{user.FirstName} {user.LastName}");
        return new AuthResponseDto(token, new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }
}
