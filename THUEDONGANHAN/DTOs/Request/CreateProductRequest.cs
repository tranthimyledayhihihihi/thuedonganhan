namespace THUEDONGANHAN.DTOs.Request
{
    public class CreateProductRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? PricePerHour { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal? PricePerWeek { get; set; }
        public decimal? PricePerMonth { get; set; }
        public decimal Deposit { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public int CategoryId { get; set; }
        public int OwnerId { get; set; }
        public string? UnavailableDates { get; set; }
        
        // ✅ FIX: Thêm các trường cho tính năng bán
        public string ProductType { get; set; } = "Rent"; // Rent | Sale | Both
        public bool IsForSale { get; set; } = false;
        public decimal? SalePrice { get; set; }
        
        public List<string> ProductImages { get; set; } = new List<string>();
    }
}
