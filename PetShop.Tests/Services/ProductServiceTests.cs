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
    public class ProductServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductService _productService;
        private readonly Mock<ILogger<ProductService>> _loggerMock;

        public ProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ProductService>>();
            _productService = new ProductService(_context, _loggerMock.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Test Hondenvoer",
                    Description = "Premium hondenvoer voor test",
                    Price = 25.99m,
                    Category = "Hondenvoer",
                    StockQuantity = 50,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Test Kattenspeelgoed",
                    Description = "Leuk speelgoed voor katten",
                    Price = 12.99m,
                    Category = "Kattenspeelgoed",
                    StockQuantity = 30,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Inactive Product",
                    Description = "Dit product is niet actief",
                    Price = 10.00m,
                    Category = "Test",
                    StockQuantity = 0,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Products.AddRange(products);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProductsAsync_ReturnsActiveProductsOnly()
        {
            // Arrange
            var searchDto = new ProductSearchDto { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count); // Only active products
            Assert.All(result.Items, p => Assert.True(p.IsActive));
        }

        [Fact]
        public async Task GetProductsAsync_WithSearchTerm_ReturnsFilteredProducts()
        {
            // Arrange
            var searchDto = new ProductSearchDto 
            { 
                SearchTerm = "Hond", 
                PageNumber = 1, 
                PageSize = 10 
            };

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Contains("Hond", result.Items[0].Name);
        }

        [Fact]
        public async Task GetProductsAsync_WithCategory_ReturnsFilteredProducts()
        {
            // Arrange
            var searchDto = new ProductSearchDto 
            { 
                Category = "Hondenvoer", 
                PageNumber = 1, 
                PageSize = 10 
            };

            // Act
            var result = await _productService.GetProductsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Hondenvoer", result.Items[0].Category);
        }

        [Fact]
        public async Task GetProductByIdAsync_ExistingProduct_ReturnsProduct()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Hondenvoer", result.Name);
        }

        [Fact]
        public async Task GetProductByIdAsync_NonExistingProduct_ReturnsNull()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProductByIdAsync_InactiveProduct_ReturnsNull()
        {
            // Act
            var result = await _productService.GetProductByIdAsync(3);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateProductAsync_ValidProduct_CreatesProduct()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "Nieuw Product",
                Description = "Een nieuw test product",
                Price = 19.99m,
                Category = "Test",
                StockQuantity = 25
            };

            // Act
            var result = await _productService.CreateProductAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Price, result.Price);
            Assert.True(result.IsActive);

            // Verify it was actually saved to the database
            var savedProduct = await _context.Products.FindAsync(result.Id);
            Assert.NotNull(savedProduct);
        }

        [Fact]
        public async Task UpdateProductAsync_ExistingProduct_UpdatesProduct()
        {
            // Arrange
            var updateDto = new UpdateProductDto
            {
                Name = "Bijgewerkt Product",
                Description = "Bijgewerkte beschrijving",
                Price = 29.99m,
                Category = "Bijgewerkt",
                StockQuantity = 40,
                IsActive = true
            };

            // Act
            var result = await _productService.UpdateProductAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Price, result.Price);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task UpdateProductAsync_NonExistingProduct_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateProductDto
            {
                Name = "Test",
                Description = "Test",
                Price = 10.00m,
                Category = "Test",
                StockQuantity = 10
            };

            // Act
            var result = await _productService.UpdateProductAsync(999, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProductAsync_ExistingProduct_SoftDeletesProduct()
        {
            // Act
            var result = await _productService.DeleteProductAsync(1);

            // Assert
            Assert.True(result);

            // Verify soft delete
            var product = await _context.Products.FindAsync(1);
            Assert.NotNull(product);
            Assert.False(product.IsActive);
            Assert.NotNull(product.UpdatedAt);
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistingProduct_ReturnsFalse()
        {
            // Act
            var result = await _productService.DeleteProductAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsActiveCategories()
        {
            // Act
            var result = await _productService.GetCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("Hondenvoer", result);
            Assert.Contains("Kattenspeelgoed", result);
            Assert.DoesNotContain("Test", result); // Inactive product category
        }

        [Fact]
        public async Task UpdateStockAsync_ExistingProduct_UpdatesStock()
        {
            // Act
            var result = await _productService.UpdateStockAsync(1, 100);

            // Assert
            Assert.True(result);

            // Verify stock was updated
            var product = await _context.Products.FindAsync(1);
            Assert.NotNull(product);
            Assert.Equal(100, product.StockQuantity);
            Assert.NotNull(product.UpdatedAt);
        }

        [Fact]
        public async Task UpdateStockAsync_NonExistingProduct_ReturnsFalse()
        {
            // Act
            var result = await _productService.UpdateStockAsync(999, 100);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
