using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dierenwinkel.Services.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(100)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCountry { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
