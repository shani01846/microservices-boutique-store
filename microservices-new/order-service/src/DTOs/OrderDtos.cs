namespace OrderService.DTOs;

public record OrderItemDto(int Id, string ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record OrderDto(int Id, DateTime OrderDate, decimal TotalAmount, string Status, List<OrderItemDto> Items);
public record CreateOrderItemDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record CreateOrderDto(List<CreateOrderItemDto> Items);
