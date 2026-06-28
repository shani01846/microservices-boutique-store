namespace ECommerce.Application.DTOs;

public record OrderDto(int Id, DateTime OrderDate, decimal TotalAmount, string Status, List<OrderItemDto> Items);

public record OrderItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderDto(List<CreateOrderItemDto> Items);

public record CreateOrderItemDto(int ProductId, int Quantity);