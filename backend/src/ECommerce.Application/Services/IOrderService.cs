using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId, bool isAdmin);
    Task<OrderDto> PlaceOrderAsync(int userId, CreateOrderDto dto);
    Task<OrderDto> PlaceOrderFromCartAsync(int userId);
}
