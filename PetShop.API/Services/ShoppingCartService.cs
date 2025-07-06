using Microsoft.EntityFrameworkCore;
using PetShop.API.Data;
using PetShop.API.DTOs;
using PetShop.API.Interfaces;
using PetShop.API.Models;

namespace PetShop.API.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShoppingCartService> _logger;

        public ShoppingCartService(ApplicationDbContext context, ILogger<ShoppingCartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ShoppingCartDto> GetCartAsync(string? userId, string sessionId)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                
                var cartDto = new ShoppingCartDto
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    SessionId = cart.SessionId,
                    CreatedAt = cart.CreatedAt,
                    UpdatedAt = cart.UpdatedAt,
                    Items = cart.ShoppingCartItems.Select(item => new ShoppingCartItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        ProductImageUrl = item.Product.ImageUrl ?? "",
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        TotalPrice = item.UnitPrice * item.Quantity,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt
                    }).ToList()
                };

                cartDto.TotalAmount = cartDto.Items.Sum(item => item.TotalPrice);

                return cartDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user: {UserId}, session: {SessionId}", userId, sessionId);
                throw;
            }
        }

        public async Task<ShoppingCartDto> AddToCartAsync(string? userId, string sessionId, AddToCartDto addToCartDto)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                var product = await _context.Products.FindAsync(addToCartDto.ProductId);
                
                if (product == null || !product.IsActive)
                {
                    throw new InvalidOperationException("Product not found or inactive");
                }

                if (product.StockQuantity < addToCartDto.Quantity)
                {
                    throw new InvalidOperationException("Insufficient stock");
                }

                var existingItem = cart.ShoppingCartItems.FirstOrDefault(item => item.ProductId == addToCartDto.ProductId);
                
                if (existingItem != null)
                {
                    // Update existing item
                    existingItem.Quantity += addToCartDto.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add new item
                    var cartItem = new ShoppingCartItem
                    {
                        ShoppingCartId = cart.Id,
                        ProductId = addToCartDto.ProductId,
                        Quantity = addToCartDto.Quantity,
                        UnitPrice = product.Price,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    cart.ShoppingCartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetCartAsync(userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user: {UserId}, session: {SessionId}", 
                    addToCartDto.ProductId, userId, sessionId);
                throw;
            }
        }

        public async Task<ShoppingCartDto> UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, UpdateCartItemDto updateCartItemDto)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                var cartItem = cart.ShoppingCartItems.FirstOrDefault(item => item.Id == cartItemId);
                
                if (cartItem == null)
                {
                    throw new InvalidOperationException("Cart item not found");
                }

                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product == null || !product.IsActive)
                {
                    throw new InvalidOperationException("Product not found or inactive");
                }

                if (product.StockQuantity < updateCartItemDto.Quantity)
                {
                    throw new InvalidOperationException("Insufficient stock");
                }

                cartItem.Quantity = updateCartItemDto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return await GetCartAsync(userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId} for user: {UserId}, session: {SessionId}", 
                    cartItemId, userId, sessionId);
                throw;
            }
        }

        public async Task<bool> RemoveFromCartAsync(string? userId, string sessionId, int cartItemId)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                var cartItem = cart.ShoppingCartItems.FirstOrDefault(item => item.Id == cartItemId);
                
                if (cartItem == null)
                {
                    return false;
                }

                cart.ShoppingCartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId} for user: {UserId}, session: {SessionId}", 
                    cartItemId, userId, sessionId);
                throw;
            }
        }

        public async Task<bool> ClearCartAsync(string? userId, string sessionId)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                cart.ShoppingCartItems.Clear();
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user: {UserId}, session: {SessionId}", userId, sessionId);
                throw;
            }
        }

        public async Task<ShoppingCartDto> MergeCartsAsync(string userId, string sessionId)
        {
            try
            {
                var userCart = await _context.ShoppingCarts
                    .Include(c => c.ShoppingCartItems)
                    .ThenInclude(item => item.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var sessionCart = await _context.ShoppingCarts
                    .Include(c => c.ShoppingCartItems)
                    .ThenInclude(item => item.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);

                if (sessionCart == null || !sessionCart.ShoppingCartItems.Any())
                {
                    return await GetCartAsync(userId, sessionId);
                }

                if (userCart == null)
                {
                    // No user cart exists, convert session cart to user cart
                    sessionCart.UserId = userId;
                    sessionCart.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Merge session cart items into user cart
                    foreach (var sessionItem in sessionCart.ShoppingCartItems)
                    {
                        var existingItem = userCart.ShoppingCartItems
                            .FirstOrDefault(item => item.ProductId == sessionItem.ProductId);

                        if (existingItem != null)
                        {
                            existingItem.Quantity += sessionItem.Quantity;
                            existingItem.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            var newItem = new ShoppingCartItem
                            {
                                ShoppingCartId = userCart.Id,
                                ProductId = sessionItem.ProductId,
                                Quantity = sessionItem.Quantity,
                                UnitPrice = sessionItem.UnitPrice,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            userCart.ShoppingCartItems.Add(newItem);
                        }
                    }

                    userCart.UpdatedAt = DateTime.UtcNow;

                    // Remove session cart
                    _context.ShoppingCarts.Remove(sessionCart);
                }

                await _context.SaveChangesAsync();
                return await GetCartAsync(userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging carts for user: {UserId}, session: {SessionId}", userId, sessionId);
                throw;
            }
        }

        public async Task<int> GetCartItemCountAsync(string? userId, string sessionId)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(userId, sessionId);
                return cart.ShoppingCartItems.Sum(item => item.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user: {UserId}, session: {SessionId}", userId, sessionId);
                throw;
            }
        }

        private async Task<ShoppingCart> GetOrCreateCartAsync(string? userId, string sessionId)
        {
            ShoppingCart? cart = null;

            if (!string.IsNullOrEmpty(userId))
            {
                cart = await _context.ShoppingCarts
                    .Include(c => c.ShoppingCartItems)
                    .ThenInclude(item => item.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (cart == null)
            {
                cart = await _context.ShoppingCarts
                    .Include(c => c.ShoppingCartItems)
                    .ThenInclude(item => item.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
    }
}
