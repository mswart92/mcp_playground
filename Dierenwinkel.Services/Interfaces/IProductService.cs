using Dierenwinkel.Services.DTOs;

namespace Dierenwinkel.Services.Interfaces
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductSearchDto searchDto);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<List<string>> GetCategoriesAsync();
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<List<ProductDto>> GetProductsByCategoryAsync(string category);
    }
}
