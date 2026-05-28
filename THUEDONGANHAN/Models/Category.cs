using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
