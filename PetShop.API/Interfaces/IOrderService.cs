using PetShop.API.DTOs;

namespace PetShop.API.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(string? userId, string sessionId, CreateOrderDto createOrderDto);
        Task<OrderDto?> GetOrderByIdAsync(int id, string? userId = null);
        Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(string? userId, int pageNumber = 1, int pageSize = 10);
        Task<List<TopProductDto>> GetTopProductsAsync(int count = 10);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<List<OrderDto>> GetOrdersByUserAsync(string userId);
        Task<bool> CancelOrderAsync(int orderId, string? userId = null);
    }
}
