using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1-5 sao

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
