using PetShop.API.DTOs;

namespace PetShop.API.Interfaces
{
    public interface IShoppingCartService
    {
        Task<ShoppingCartDto> GetCartAsync(string? userId, string sessionId);
        Task<ShoppingCartDto> AddToCartAsync(string? userId, string sessionId, AddToCartDto addToCartDto);
        Task<ShoppingCartDto> UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, UpdateCartItemDto updateCartItemDto);
        Task<bool> RemoveFromCartAsync(string? userId, string sessionId, int cartItemId);
        Task<bool> ClearCartAsync(string? userId, string sessionId);
        Task<ShoppingCartDto> MergeCartsAsync(string userId, string sessionId);
        Task<int> GetCartItemCountAsync(string? userId, string sessionId);
    }
}
