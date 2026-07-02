namespace CartService.DTOs;

public record CartItemDto(string ProductId, string ProductName, decimal Price, int Quantity, decimal Subtotal);
public record CartDto(string UserId, List<CartItemDto> Items, decimal Total);
public record AddToCartDto(string ProductId, string ProductName, decimal Price, int Quantity);
public record UpdateCartItemDto(int Quantity);
