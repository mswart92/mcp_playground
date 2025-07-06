using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PetShop.API.Data;
using PetShop.API.DTOs;
using PetShop.API.Interfaces;
using PetShop.API.Models;
using PetShop.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PetShop.Tests.Services
{
    public class OrderServiceTests
    {
        private DbContextOptions<ApplicationDbContext> GetInMemoryDbOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private ApplicationDbContext CreateDbContext()
        {
            var options = GetInMemoryDbOptions();
            var context = new ApplicationDbContext(options);
            
            // Seed test data
            SeedTestData(context);
            
            return context;
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            var user = new ApplicationUser
            {
                Id = "user1",
                Email = "test@example.com",
                UserName = "testuser",
                FirstName = "Test",
                LastName = "User"
            };

            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Dog Food", Price = 25.99m, StockQuantity = 50, Category = "Food" },
                new Product { Id = 2, Name = "Cat Toy", Price = 12.99m, StockQuantity = 20, Category = "Toys" },
                new Product { Id = 3, Name = "Bird Cage", Price = 89.99m, StockQuantity = 5, Category = "Housing" }
            };

            context.Users.Add(user);
            context.Products.AddRange(products);
            context.SaveChanges();
        }

        [Fact]
        public async Task CreateOrderAsync_WithValidCart_ShouldCreateOrder()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            var cartItems = new List<ShoppingCartItemDto>
            {
                new ShoppingCartItemDto { ProductId = 1, Quantity = 2 },
                new ShoppingCartItemDto { ProductId = 2, Quantity = 1 }
            };

            var createOrderDto = new CreateOrderDto
            {
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                Items = cartItems
            };

            // Act
            var result = await service.CreateOrderAsync("user1", createOrderDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("user1", result.UserId);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(64.97m, result.TotalAmount); // (25.99 * 2) + (12.99 * 1)
            Assert.Equal(OrderStatus.Pending, result.Status);

            // Verify order was saved to database
            var savedOrder = await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == result.Id);
            Assert.NotNull(savedOrder);
            Assert.Equal(2, savedOrder.Items.Count);

            // Verify stock was updated
            var product1 = await context.Products.FindAsync(1);
            var product2 = await context.Products.FindAsync(2);
            Assert.Equal(48, product1.StockQuantity); // 50 - 2
            Assert.Equal(19, product2.StockQuantity); // 20 - 1
        }

        [Fact]
        public async Task CreateOrderAsync_WithInsufficientStock_ShouldThrowException()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            var cartItems = new List<ShoppingCartItemDto>
            {
                new ShoppingCartItemDto { ProductId = 3, Quantity = 10 } // Only 5 in stock
            };

            var createOrderDto = new CreateOrderDto
            {
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                Items = cartItems
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateOrderAsync("user1", createOrderDto));
        }

        [Fact]
        public async Task GetOrdersByUserIdAsync_ShouldReturnUserOrders()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Create test order
            var order = new Order
            {
                UserId = "user1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Pending,
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                CreatedAt = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetOrdersByUserIdAsync("user1");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("user1", result.First().UserId);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithValidId_ShouldReturnOrder()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Create test order
            var order = new Order
            {
                UserId = "user1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Pending,
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                CreatedAt = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetOrderByIdAsync(order.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
            Assert.Equal("user1", result.UserId);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Act
            var result = await service.GetOrderByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTop10ProductsAsync_ShouldReturnTopProducts()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Create test orders with items
            var order1 = new Order
            {
                UserId = "user1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 5, Price = 25.99m },
                    new OrderItem { ProductId = 2, Quantity = 2, Price = 12.99m }
                }
            };

            var order2 = new Order
            {
                UserId = "user1",
                TotalAmount = 50.00m,
                Status = OrderStatus.Completed,
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 3, Price = 25.99m },
                    new OrderItem { ProductId = 3, Quantity = 1, Price = 89.99m }
                }
            };

            context.Orders.AddRange(order1, order2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTop10ProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Product 1 should be first (8 total quantity)
            var topProduct = result.First();
            Assert.Equal(1, topProduct.Id);
            Assert.Equal("Dog Food", topProduct.Name);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithValidId_ShouldUpdateStatus()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Create test order
            var order = new Order
            {
                UserId = "user1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Pending,
                ShippingAddress = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country",
                CreatedAt = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

            // Assert
            Assert.True(result);
            
            // Verify status was updated
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Processing, updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            using var context = CreateDbContext();
            var mockLogger = new Mock<ILogger<OrderService>>();
            var mockEmailService = new Mock<IEmailService>();
            var service = new OrderService(context, mockLogger.Object, mockEmailService.Object);

            // Act
            var result = await service.UpdateOrderStatusAsync(999, OrderStatus.Processing);

            // Assert
            Assert.False(result);
        }
    }
}
