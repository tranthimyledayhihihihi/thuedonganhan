using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // Deposit, Withdraw, Payment, Refund, Commission

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int? ReferenceId { get; set; } // Can be RentalId

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
