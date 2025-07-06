using Microsoft.EntityFrameworkCore;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.Models;
using Dierenwinkel.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Dierenwinkel.Services.DTOs;

namespace PetShop.Tests.Services
{
    public class ShoppingCartServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ShoppingCartService _shoppingCartService;
        private readonly Mock<ILogger<ShoppingCartService>> _loggerMock;

        public ShoppingCartServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ShoppingCartService>>();
            _shoppingCartService = new ShoppingCartService(_context, _loggerMock.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Description = "Test beschrijving",
                Price = 19.99m,
                Category = "Test",
                StockQuantity = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetCartAsync_NewSession_CreatesEmptyCart()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();

            // Act
            var result = await _shoppingCartService.GetCartAsync(null, sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalAmount);
        }

        [Fact]
        public async Task AddToCartAsync_ValidProduct_AddsToCart()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 2
            };

            // Act
            var result = await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(2, result.Items[0].Quantity);
            Assert.Equal(19.99m, result.Items[0].UnitPrice);
            Assert.Equal(39.98m, result.Items[0].TotalPrice);
            Assert.Equal(39.98m, result.TotalAmount);
        }

        [Fact]
        public async Task AddToCartAsync_ExistingProduct_UpdatesQuantity()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 2
            };

            // Add first time
            await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);

            // Act - Add same product again
            var result = await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(4, result.Items[0].Quantity); // 2 + 2
            Assert.Equal(79.96m, result.Items[0].TotalPrice);
            Assert.Equal(79.96m, result.TotalAmount);
        }

        [Fact]
        public async Task AddToCartAsync_InactiveProduct_ThrowsException()
        {
            // Arrange
            var inactiveProduct = new Product
            {
                Id = 2,
                Name = "Inactive Product",
                Price = 10.00m,
                StockQuantity = 10,
                IsActive = false
            };
            _context.Products.Add(inactiveProduct);
            _context.SaveChanges();

            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 2,
                Quantity = 1
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto));
        }

        [Fact]
        public async Task AddToCartAsync_InsufficientStock_ThrowsException()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 150 // More than available stock (100)
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto));
        }

        [Fact]
        public async Task UpdateCartItemAsync_ValidQuantity_UpdatesQuantity()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 2
            };

            // Add to cart first
            var cart = await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);
            var cartItemId = cart.Items[0].Id;

            var updateDto = new UpdateCartItemDto
            {
                Quantity = 5
            };

            // Act
            var result = await _shoppingCartService.UpdateCartItemAsync(null, sessionId, cartItemId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(5, result.Items[0].Quantity);
            Assert.Equal(99.95m, result.Items[0].TotalPrice);
        }

        [Fact]
        public async Task RemoveFromCartAsync_ExistingItem_RemovesItem()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 2
            };

            // Add to cart first
            var cart = await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);
            var cartItemId = cart.Items[0].Id;

            // Act
            var result = await _shoppingCartService.RemoveFromCartAsync(null, sessionId, cartItemId);

            // Assert
            Assert.True(result);

            // Verify item was removed
            var updatedCart = await _shoppingCartService.GetCartAsync(null, sessionId);
            Assert.Empty(updatedCart.Items);
        }

        [Fact]
        public async Task ClearCartAsync_WithItems_ClearsAllItems()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 2
            };

            // Add to cart first
            await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);

            // Act
            var result = await _shoppingCartService.ClearCartAsync(null, sessionId);

            // Assert
            Assert.True(result);

            // Verify cart was cleared
            var clearedCart = await _shoppingCartService.GetCartAsync(null, sessionId);
            Assert.Empty(clearedCart.Items);
        }

        [Fact]
        public async Task GetCartItemCountAsync_WithItems_ReturnsCorrectCount()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var addToCartDto = new AddToCartDto
            {
                ProductId = 1,
                Quantity = 3
            };

            // Add to cart first
            await _shoppingCartService.AddToCartAsync(null, sessionId, addToCartDto);

            // Act
            var result = await _shoppingCartService.GetCartItemCountAsync(null, sessionId);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task GetCartItemCountAsync_EmptyCart_ReturnsZero()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();

            // Act
            var result = await _shoppingCartService.GetCartItemCountAsync(null, sessionId);

            // Assert
            Assert.Equal(0, result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
