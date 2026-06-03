namespace WEB.Models
{
    // ViewModel cho tổng quan Dashboard Admin
    public class AdminDashboardViewModel
    {
        public AdminStats Stats { get; set; } = new();
        public List<AdminCommissionTransaction> RecentCommissions { get; set; } = new();
    }

    public class AdminStats
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalRentals { get; set; }
        public int ActiveRentals { get; set; }
        public int PendingRentals { get; set; }
        public int CompletedRentals { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal AdminBalance { get; set; }
    }

    public class AdminCommissionTransaction
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ViewModel cho trang quản lý người dùng
    public class AdminUserViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public int RentalCount { get; set; }
        public int ProductCount { get; set; }
        public DateTime? LockEnd { get; set; }
        public string? LockReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ViewModel cho trang quản lý đơn thuê
    public class AdminRentalViewModel
    {
        public int RentalId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal Commission { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string RenterName { get; set; } = string.Empty;
        public string RenterEmail { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
    }

    // ViewModel cho trang hoa hồng
    public class AdminCommissionsViewModel
    {
        public List<AdminCommissionTransaction> Items { get; set; } = new();
        public decimal AdminBalance { get; set; }
        public AdminPagination Pagination { get; set; } = new();
    }

    // ViewModel cho trang sản phẩm
    public class AdminProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal Deposit { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsApproved { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int RentalCount { get; set; }
    }

    // Generic paged result
    public class AdminPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public AdminPagination Pagination { get; set; } = new();
    }

    public class AdminPagination
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // ViewModel cho sinh viên
    public class AdminStudentViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Major { get; set; }
        public string? Faculty { get; set; }
        public string? ClassName { get; set; }
        public int? AcademicYear { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasAccount { get; set; }
        public AdminStudentUserInfo? UserInfo { get; set; }
    }

    public class AdminStudentUserInfo
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }

    // ViewModel cho đơn mua
    public class AdminSaleOrderViewModel
    {
        public int SaleOrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public decimal Commission { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public int? PreviousOwnerId { get; set; }
        public int CurrentOwnerId { get; set; }
        public string? Notes { get; set; }
    }

    // ViewModel cho thanh toán
    public class AdminPaymentViewModel
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public string PayerEmail { get; set; } = string.Empty;
        public int? RentalId { get; set; }
        public int? SaleOrderId { get; set; }
        public string? Notes { get; set; }
    }

    // ViewModel cho giao dịch
    public class AdminTransactionViewModel
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ViewModel cho danh mục
    public class AdminCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }

    // ViewModel cho đánh giá
    public class AdminReviewViewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ViewModel cho thống kê chi tiết
    public class AdminStatisticsViewModel
    {
        public AdminStatUsers Users { get; set; } = new();
        public AdminStatStudents Students { get; set; } = new();
        public AdminStatProducts Products { get; set; } = new();
        public AdminStatRentals Rentals { get; set; } = new();
        public AdminStatSaleOrders SaleOrders { get; set; } = new();
        public AdminStatPayments Payments { get; set; } = new();
        public AdminStatRevenue Revenue { get; set; } = new();
    }

    public class AdminStatUsers
    {
        public int Total { get; set; }
        public int Verified { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
    }

    public class AdminStatStudents
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int WithAccount { get; set; }
    }

    public class AdminStatProducts
    {
        public int Total { get; set; }
        public int Available { get; set; }
        public int ForRent { get; set; }
        public int ForSale { get; set; }
    }

    public class AdminStatRentals
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Confirmed { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class AdminStatSaleOrders
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Completed { get; set; }
    }

    public class AdminStatPayments
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
    }

    public class AdminStatRevenue
    {
        public decimal TotalRentalRevenue { get; set; }
        public decimal TotalSaleRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal AdminBalance { get; set; }
    }
}
