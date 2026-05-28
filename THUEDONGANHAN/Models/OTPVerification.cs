using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class OTPVerification
    {
        [Key]
        public int OTPId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(10)]
        public string OTPCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string OTPType { get; set; } = "EmailVerification"; // EmailVerification | PasswordReset

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
