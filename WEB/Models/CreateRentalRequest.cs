using System;

namespace WEB.Models
{
    public class CreateRentalRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string RentalUnit { get; set; } = "Day";
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
    }
}
