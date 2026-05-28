namespace THUEDONGANHAN.DTOs.Response
{
    /// <summary>
    /// DTO cho SaleOrder response - tránh circular reference và lộ thông tin nhạy cảm
    /// </summary>
    public class SaleOrderResponse
    {
        public int SaleOrderId { get; set; }
        public int ProductId { get; set; }
        public int BuyerId { get; set; }
        public decimal SalePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? PreviousOwnerId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Nested DTOs - chỉ thông tin cần thiết
        public ProductSummary? Product { get; set; }
        public UserSummary? Buyer { get; set; }
    }

    public class ProductSummary
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public int OwnerId { get; set; }
    }

    public class UserSummary
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
