using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Rental
    {
        [Key]
        public int RentalId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int RenterId { get; set; }

        public int Quantity { get; set; } = 1;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime? ActualReturnDate { get; set; }

        [MaxLength(10)]
        public string RentalUnit { get; set; } = "Day"; // Hour | Day | Week | Month

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; } = 0;

        public bool DepositRefunded { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DamageFee { get; set; } = 0;

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending | Confirmed | InProgress | Completed | Cancelled | Disputed

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? OwnerNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("RenterId")]
        public virtual User Renter { get; set; } = null!;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
