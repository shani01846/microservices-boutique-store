namespace ECommerce.Application.DTOs;

public record UserDto(int Id, string Email, string FirstName, string LastName, string Role);

public record RegisterUserDto(string Email, string Password, string FirstName, string LastName);

public record LoginUserDto(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

public record CartDto(int Id, int UserId, List<CartItemDto> Items, decimal Total);

public record CartItemDto(int Id, int ProductId, string ProductName, decimal Price, int Quantity, decimal Subtotal);

public record AddToCartDto(int ProductId, int Quantity);