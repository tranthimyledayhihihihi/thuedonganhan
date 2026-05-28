using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AltText { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
