using System;

namespace WEB.Models
{
    public class TransactionViewModel
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty; // Deposit, Withdraw, Payment, Refund, Commission
        public decimal Amount { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
