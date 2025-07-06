using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dierenwinkel.Services.DTOs;
using Dierenwinkel.Services.Interfaces;

namespace PetShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Haal alle producten op met zoek- en filtermogelijkheden
        /// </summary>
        /// <param name="searchDto">Zoek en filter parameters</param>
        /// <returns>Paginated lijst van producten</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProducts([FromQuery] ProductSearchDto searchDto)
        {
            try
            {
                var result = await _productService.GetProductsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van producten" });
            }
        }

        /// <summary>
        /// Haal een specifiek product op
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Product niet gevonden" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with ID: {ProductId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van het product" });
            }
        }

        /// <summary>
        /// Haal alle beschikbare categorieën op
        /// </summary>
        /// <returns>Lijst van categorieën</returns>
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                var categories = await _productService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van categorieën" });
            }
        }

        /// <summary>
        /// Haal producten op per categorie
        /// </summary>
        /// <param name="category">Categorie naam</param>
        /// <returns>Lijst van producten in de categorie</returns>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<List<ProductDto>>> GetProductsByCategory(string category)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryAsync(category);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {Category}", category);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van producten per categorie" });
            }
        }

        /// <summary>
        /// Maak een nieuw product aan (alleen voor Admin)
        /// </summary>
        /// <param name="createProductDto">Product gegevens</param>
        /// <returns>Nieuw product</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _productService.CreateProductAsync(createProductDto);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het aanmaken van het product" });
            }
        }

        /// <summary>
        /// Update een product (alleen voor Admin)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateProductDto">Bijgewerkte product gegevens</param>
        /// <returns>Bijgewerkt product</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _productService.UpdateProductAsync(id, updateProductDto);
                if (product == null)
                {
                    return NotFound(new { message = "Product niet gevonden" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het bijwerken van het product" });
            }
        }

        /// <summary>
        /// Verwijder een product (alleen voor Admin)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Geen content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Product niet gevonden" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het verwijderen van het product" });
            }
        }

        /// <summary>
        /// Update voorraad van een product (alleen voor Admin)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="quantity">Nieuwe voorraad hoeveelheid</param>
        /// <returns>Geen content</returns>
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateStock(int id, [FromBody] int quantity)
        {
            try
            {
                var success = await _productService.UpdateStockAsync(id, quantity);
                if (!success)
                {
                    return NotFound(new { message = "Product niet gevonden" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product ID: {ProductId}", id);
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het bijwerken van de voorraad" });
            }
        }
    }
}
