using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dierenwinkel.Services.Models
{
    public class ShoppingCart
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; } // Nullable voor anonieme winkelwagens

        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
    }
}
