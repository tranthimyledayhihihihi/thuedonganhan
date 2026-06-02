using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Complaint
    {
        [Key]
        public int ComplaintId { get; set; }

        public int RentalId { get; set; }
        
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Resolving, Resolved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RentalId")]
        public virtual Rental? Rental { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
