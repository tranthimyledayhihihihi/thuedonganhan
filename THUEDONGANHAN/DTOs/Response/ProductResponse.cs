namespace THUEDONGANHAN.DTOs.Response
{
    public class ProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? PricePerHour { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal? PricePerWeek { get; set; }
        public decimal? PricePerMonth { get; set; }
        public decimal Deposit { get; set; }
        
        // ✅ TÍNH NĂNG BÁN
        public string ProductType { get; set; } = "Rent";
        public bool IsForSale { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal AverageRating { get; set; }
        
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public string? UnavailableDates { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public int CategoryId { get; set; }
        
        // Thông tin Category (không có Products để tránh circular reference)
        public CategoryResponse? Category { get; set; }
        
        // Thông tin Owner (không có Products để tránh circular reference)
        public OwnerResponse? Owner { get; set; }
        
        public List<string> ProductImages { get; set; } = new List<string>();
    }

    public class CategoryResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class OwnerResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
