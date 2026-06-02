namespace WEB.Models
{
    public class CreateProductRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? PricePerHour { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal? PricePerWeek { get; set; }
        public decimal? PricePerMonth { get; set; }
        public decimal? Deposit { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public int CategoryId { get; set; }
        public int OwnerId { get; set; }
        
        // Lịch không cho thuê (JSON string: ["2024-01-15", "2024-01-20"])
        public string? UnavailableDates { get; set; }
        
        public string ProductType { get; set; } = "Rent";
        public bool IsForSale { get; set; } = false;
        public decimal? SalePrice { get; set; }

        public List<string> ProductImages { get; set; } = new List<string>();
        public string? ProductImagesJson { get; set; }
    }
}
