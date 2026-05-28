namespace WEB.Models
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
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
        public bool IsAvailable { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public int CategoryId { get; set; }
        
        // Navigation properties
        public CategoryViewModel? Category { get; set; }
        public UserViewModel? Owner { get; set; }
    }

    public class UserViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
