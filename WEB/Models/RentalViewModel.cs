using System;

namespace WEB.Models
{
    public class RentalViewModel
    {
        public int RentalId { get; set; }
        public int ProductId { get; set; }
        public int RenterId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string RentalUnit { get; set; } = "Day";
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public bool DepositRefunded { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ProductViewModel? Product { get; set; }
        public UserViewModel? Renter { get; set; }

        // Helper: hiển thị trạng thái tiếng Việt
        public string StatusDisplay => Status switch
        {
            "Pending" => "Chờ xác nhận",
            "Confirmed" => "Đã xác nhận",
            "Active" => "Đang cho thuê",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            "Disputed" => "Tranh chấp",
            _ => Status
        };

        public string StatusBadgeClass => Status switch
        {
            "Pending" => "badge-warning",
            "Confirmed" => "badge-info",
            "Active" => "badge-success",
            "Completed" => "badge-primary",
            "Cancelled" => "badge-danger",
            "Disputed" => "badge-secondary",
            _ => "badge-secondary"
        };
    }
}
