using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class SaleOrder
    {
        [Key]
        public int SaleOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int BuyerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; } // ✅ FIX: Đổi từ UnitPrice/TotalPrice thành SalePrice

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending | Confirmed | Completed | Cancelled

        public int? PreviousOwnerId { get; set; } // ✅ FIX: Thêm PreviousOwnerId

        [MaxLength(500)]
        public string? Notes { get; set; } // ✅ FIX: Đổi từ Note thành Notes

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } // ✅ FIX: Thêm UpdatedAt

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; } = null!;
    }
}
