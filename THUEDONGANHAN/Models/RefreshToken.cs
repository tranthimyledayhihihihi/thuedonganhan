using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class RefreshToken
    {
        [Key]
        public int TokenId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
