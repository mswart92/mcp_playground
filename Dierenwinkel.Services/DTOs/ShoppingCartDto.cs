using System.ComponentModel.DataAnnotations;

namespace Dierenwinkel.Services.DTOs
{
    public class ShoppingCartDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public List<ShoppingCartItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ShoppingCartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public string? SessionId { get; set; }
    }

    public class UpdateCartItemDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
