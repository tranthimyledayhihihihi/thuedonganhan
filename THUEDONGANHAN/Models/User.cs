using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "User"; // User | Admin

        [MaxLength(20)]
        public string UserType { get; set; } = "Both"; // Renter | Owner | Both

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0m;

        public bool IsActive { get; set; } = true;

        public bool IsVerified { get; set; } = false;

        public int? StudentId { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
        
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Rental> RentalsAsRenter { get; set; } = new List<Rental>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
