using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Condition { get; set; } = "Good"; // New | LikeNew | Good | Fair | Poor

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerHour { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerWeek { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerMonth { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deposit { get; set; } = 0;

        // ✅ TÍNH NĂNG BÁN
        [MaxLength(20)]
        public string ProductType { get; set; } = "Rent"; // Rent | Sale | Both

        public bool IsForSale { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        public int Quantity { get; set; } = 1;

        public int AvailableQuantity { get; set; } = 1; // Số lượng còn cho thuê

        [MaxLength(500)]
        public string? ImageUrl { get; set; } // Ảnh đại diện chính

        [MaxLength(255)]
        public string? Location { get; set; }

        [NotMapped]
        public string? UnavailableDates { get; set; } // JSON: ["2026-05-20","2026-05-21"]

        public bool IsAvailable { get; set; } = true;
        public bool IsApproved { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public int RentCount { get; set; } = 0;

        public int SaleCount { get; set; } = 0;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        [Required]
        public int OwnerId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        // Navigation Properties
        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
