using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Data;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;
using Dierenwinkel.Services.Models;

namespace Dierenwinkel.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductSearchDto searchDto)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    query = query.Where(p => p.Name.Contains(searchDto.SearchTerm) || 
                                           p.Description.Contains(searchDto.SearchTerm));
                }

                if (!string.IsNullOrEmpty(searchDto.Category))
                {
                    query = query.Where(p => p.Category == searchDto.Category);
                }

                if (searchDto.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= searchDto.MinPrice.Value);
                }

                if (searchDto.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= searchDto.MaxPrice.Value);
                }

                // Only show active products
                query = query.Where(p => p.IsActive);

                // Apply sorting
                query = searchDto.SortBy?.ToLower() switch
                {
                    "name" => searchDto.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                    "price" => searchDto.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                    "category" => searchDto.SortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                    "createdat" => searchDto.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.Name)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)searchDto.PageSize);

                var products = await query
                    .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.Category,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return new PagedResultDto<ProductDto>
                {
                    Items = products,
                    TotalCount = totalCount,
                    PageNumber = searchDto.PageNumber,
                    PageSize = searchDto.PageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = searchDto.PageNumber > 1,
                    HasNextPage = searchDto.PageNumber < totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products with search criteria");
                throw;
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.Id == id && p.IsActive)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.Category,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            try
            {
                var product = new Product
                {
                    Name = createProductDto.Name,
                    Description = createProductDto.Description,
                    Price = createProductDto.Price,
                    Category = createProductDto.Category,
                    StockQuantity = createProductDto.StockQuantity,
                    ImageUrl = createProductDto.ImageUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Category = product.Category,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return null;
                }

                product.Name = updateProductDto.Name;
                product.Description = updateProductDto.Description;
                product.Price = updateProductDto.Price;
                product.Category = updateProductDto.Category;
                product.StockQuantity = updateProductDto.StockQuantity;
                product.ImageUrl = updateProductDto.ImageUrl;
                product.IsActive = updateProductDto.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Category = product.Category,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return false;
                }

                // Soft delete - set IsActive to false
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return false;
                }

                product.StockQuantity = quantity;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product ID: {ProductId}", productId);
                throw;
            }
        }

        public async Task<List<ProductDto>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Category == category && p.IsActive)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.Category,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {Category}", category);
                throw;
            }
        }
    }
}
