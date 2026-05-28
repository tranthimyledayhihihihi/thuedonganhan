using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int? RentalId { get; set; }

        public int? SaleOrderId { get; set; }

        [Required]
        public int PayerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(20)]
        public string PaymentType { get; set; } = "RentalFee"; // RentalFee | Deposit | LateFee | DamageFee | DepositRefund | SalePayment

        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash | BankTransfer | Momo | ZaloPay | VNPay

        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending | Completed | Failed | Refunded | Cancelled

        [MaxLength(100)]
        public string? TransactionId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("RentalId")]
        public virtual Rental? Rental { get; set; }

        [ForeignKey("SaleOrderId")]
        public virtual SaleOrder? SaleOrder { get; set; }

        [ForeignKey("PayerId")]
        public virtual User Payer { get; set; } = null!;
    }
}
