using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Models;
using Dierenwinkel.Services.Services;
using Dierenwinkel.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PetShop.Tests.Integration
{
    public class ServiceIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly IServiceProvider _serviceProvider;

        public ServiceIntegrationTests()
        {
            // Setup in-memory database
            var services = new ServiceCollection();
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IProductService, ProductService>();

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            _productService = _serviceProvider.GetRequiredService<IProductService>();

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            _context.Products.AddRange(new[]
            {
                new Product
                {
                    Id = 1,
                    Name = "Test Product 1",
                    Description = "Test Description 1",
                    Price = 10.99m,
                    Category = "Test Category",
                    StockQuantity = 100,
                    ImageUrl = "test1.jpg"
                },
                new Product
                {
                    Id = 2,
                    Name = "Test Product 2",
                    Description = "Test Description 2",
                    Price = 20.99m,
                    Category = "Test Category",
                    StockQuantity = 50,
                    ImageUrl = "test2.jpg"
                }
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnProducts()
        {
            // Arrange
            var searchDto = new ProductSearchDto();

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 2);
        }

        [Fact]
        public async Task GetProductByIdAsync_WithValidId_ShouldReturnProduct()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Product 1", result.Name);
            Assert.Equal(10.99m, result.Price);
        }

        [Fact]
        public async Task GetProductByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProductsAsync_WithCategoryFilter_ShouldReturnFilteredProducts()
        {
            // Arrange
            var searchDto = new ProductSearchDto
            {
                Category = "Test Category"
            };

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count >= 2);
            Assert.All(result.Items, p => Assert.Equal("Test Category", p.Category));
        }

        [Fact]
        public async Task GetProductsAsync_WithPriceFilter_ShouldReturnFilteredProducts()
        {
            // Arrange
            var searchDto = new ProductSearchDto
            {
                MinPrice = 15.00m,
                MaxPrice = 25.00m
            };

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Test Product 2", result.Items[0].Name);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.GetService<IServiceScope>()?.Dispose();
        }
    }
}
