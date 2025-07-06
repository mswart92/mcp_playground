using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dierenwinkel.Services.Models
{
    public class ShoppingCartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShoppingCartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ShoppingCartId")]
        public virtual ShoppingCart ShoppingCart { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
