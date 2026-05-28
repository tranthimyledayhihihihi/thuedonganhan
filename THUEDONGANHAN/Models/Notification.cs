using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Type { get; set; } = "System"; // System | RentalRequest | RentalConfirmed | etc.

        public int? ReferenceId { get; set; }

        [MaxLength(30)]
        public string? ReferenceType { get; set; } // Rental | Message | Product

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
