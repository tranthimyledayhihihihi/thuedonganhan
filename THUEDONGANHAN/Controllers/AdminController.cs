using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Admin/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var totalRentals = await _context.Rentals.CountAsync();
                var activeRentals = await _context.Rentals.CountAsync(r => r.Status == "Active");
                var pendingRentals = await _context.Rentals.CountAsync(r => r.Status == "Pending");
                var completedRentals = await _context.Rentals.CountAsync(r => r.Status == "Completed");

                var totalRevenue = await _context.Rentals
                    .Where(r => r.Status == "Completed" || r.Status == "Active")
                    .SumAsync(r => r.TotalPrice);

                var totalCommission = await _context.Transactions
                    .Where(t => t.Type == "Commission")
                    .SumAsync(t => t.Amount);

                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                var adminBalance = adminUser?.Balance ?? 0;

                var recentCommissions = await _context.Transactions
                    .Where(t => t.Type == "Commission")
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.UserId,
                        t.Type,
                        t.Amount,
                        t.ReferenceId,
                        t.Description,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var stats = new
                {
                    stats = new
                    {
                        totalUsers,
                        totalProducts,
                        totalRentals,
                        activeRentals,
                        pendingRentals,
                        completedRentals,
                        totalRevenue,
                        totalCommission,
                        adminBalance
                    },
                    recentCommissions
                };

                return Ok(ApiResponse<object>.SuccessResponse(stats, "Lấy thống kê dashboard thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/users
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        u.FullName.Contains(search) ||
                        u.Email.Contains(search) ||
                        u.PhoneNumber.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        u.Role,
                        u.UserType,
                        u.Balance,
                        u.IsActive,
                        u.IsVerified,
                        u.LockEnd,
                        u.LockReason,
                        u.CreatedAt,
                        RentalCount = u.RentalsAsRenter.Count,
                        ProductCount = u.Products.Count
                    })
                    .ToListAsync();

                var result = new
                {
                    items = users,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách người dùng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/rentals
        [HttpGet("rentals")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllRentals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Rentals
                    .Include(r => r.Product)
                    .Include(r => r.Renter)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var rentals = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.RentalId,
                        r.Status,
                        r.TotalPrice,
                        r.DepositAmount,
                        Commission = Math.Round(r.TotalPrice * 0.05m, 2),
                        r.StartDate,
                        r.EndDate,
                        r.CreatedAt,
                        r.UpdatedAt,
                        ProductName = r.Product.ProductName,
                        RenterName = r.Renter.FullName,
                        RenterEmail = r.Renter.Email,
                        OwnerName = r.Product.Owner.FullName
                    })
                    .ToListAsync();

                var result = new
                {
                    items = rentals,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách đơn thuê thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rentals");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/commissions
        [HttpGet("commissions")]
        public async Task<ActionResult<ApiResponse<object>>> GetCommissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Transactions
                    .Where(t => t.Type == "Commission")
                    .OrderByDescending(t => t.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var commissions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.UserId,
                        t.Type,
                        t.Amount,
                        t.ReferenceId,
                        t.Description,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                var adminBalance = adminUser?.Balance ?? 0;

                var result = new
                {
                    items = commissions,
                    adminBalance,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách hoa hồng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commissions");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/products
        [HttpGet("products")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Products
                    .Include(p => p.Owner)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p =>
                        p.ProductName.Contains(search) ||
                        p.Owner.FullName.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        Description = p.Description,
                        ImageUrl = (!string.IsNullOrEmpty(p.ImageUrl) && p.ImageUrl.StartsWith("/uploads/"))
                            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{p.ImageUrl}"
                            : p.ImageUrl,
                        p.PricePerDay,
                        p.Deposit,
                        p.Quantity,
                        p.IsAvailable,
                        p.IsApproved,
                        p.Location,
                        p.CreatedAt,
                        OwnerName = p.Owner.FullName,
                        OwnerEmail = p.Owner.Email,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "N/A",
                        RentalCount = _context.Rentals.Count(r => r.ProductId == p.ProductId)
                    })
                    .ToListAsync();

                var result = new
                {
                    items = products,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Admin/users/{id}/toggle-active
        [HttpPut("users/{id}/toggle-active")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleUserActive(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Người dùng không tồn tại"));

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true,
                    $"Đã {(user.IsActive ? "kích hoạt" : "vô hiệu hóa")} tài khoản {user.FullName}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active status");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Admin/users/{id}/lock
        [HttpPut("users/{id}/lock")]
        public async Task<ActionResult<ApiResponse<bool>>> LockUser(int id, [FromBody] DTOs.Request.LockUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Người dùng không tồn tại"));

                user.IsActive = false;
                user.LockReason = request.Reason;
                
                if (request.Duration == "1week")
                {
                    user.LockEnd = DateTime.Now.AddDays(7);
                }
                else if (request.Duration == "1month")
                {
                    user.LockEnd = DateTime.Now.AddMonths(1);
                }
                else
                {
                    user.LockEnd = null; // Permanent
                }

                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var durationStr = request.Duration switch
                {
                    "1week" => "1 tuần",
                    "1month" => "1 tháng",
                    _ => "vĩnh viễn"
                };

                return Ok(ApiResponse<bool>.SuccessResponse(true,
                    $"Đã khóa tài khoản {user.FullName} trong {durationStr}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Admin/users/{id}/unlock
        [HttpPut("users/{id}/unlock")]
        public async Task<ActionResult<ApiResponse<bool>>> UnlockUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Người dùng không tồn tại"));

                user.IsActive = true;
                user.LockEnd = null;
                user.LockReason = null;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true,
                    $"Đã mở khóa tài khoản {user.FullName}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Admin/products/{id}
        [HttpDelete("products/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Sản phẩm không tồn tại"));

                // Kiểm tra xem có đơn thuê đang hoạt động không
                var hasActiveRentals = await _context.Rentals
                    .AnyAsync(r => r.ProductId == id && 
                        (r.Status == "Pending" || r.Status == "Confirmed" || 
                         r.Status == "InProgress" || r.Status == "Active" || r.Status == "Returned"));

                if (hasActiveRentals)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Không thể xóa sản phẩm đang có đơn thuê hoạt động."));

                // Kiểm tra xem có đơn mua đang hoạt động không
                var hasActiveSaleOrders = await _context.SaleOrders
                    .AnyAsync(so => so.ProductId == id && 
                        (so.Status == "Pending" || so.Status == "Confirmed"));

                if (hasActiveSaleOrders)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Không thể xóa sản phẩm đang có đơn mua chưa hoàn tất."));

                // Kiểm tra lịch sử giao dịch (bao gồm cả đơn đã hoàn thành/hủy)
                var hasHistory = await _context.Rentals.AnyAsync(r => r.ProductId == id) ||
                                 await _context.SaleOrders.AnyAsync(so => so.ProductId == id);

                if (hasHistory)
                {
                    // Soft-delete
                    product.IsAvailable = false;
                    product.IsApproved = false;
                    product.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<bool>.SuccessResponse(true, "Sản phẩm đã có lịch sử giao dịch, hệ thống đã chuyển trạng thái ẩn để bảo toàn dữ liệu."));
                }

                // Hard-delete
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Admin/products/{id}
        [HttpPut("products/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> EditProduct(int id, [FromBody] EditProductAdminRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Sản phẩm không tồn tại"));

                product.ProductName = request.ProductName;
                product.PricePerDay = request.PricePerDay;
                product.Deposit = request.Deposit;
                product.Quantity = request.Quantity;
                product.Location = request.Location;
                product.Description = request.Description ?? string.Empty;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing product by admin");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/students
        [HttpGet("students")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Students.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(s =>
                        s.StudentCode.Contains(search) ||
                        s.FullName.Contains(search) ||
                        s.Email.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var students = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.StudentId,
                        s.StudentCode,
                        s.FullName,
                        s.Email,
                        s.PhoneNumber,
                        s.Major,
                        s.Faculty,
                        ClassName = s.Class,
                        s.AcademicYear,
                        s.Status,
                        s.CreatedAt,
                        HasAccount = _context.Users.Any(u => u.StudentId == s.StudentId),
                        UserInfo = _context.Users
                            .Where(u => u.StudentId == s.StudentId)
                            .Select(u => new
                            {
                                u.UserId,
                                u.Email,
                                u.IsVerified,
                                u.IsActive
                            })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var result = new
                {
                    items = students,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách sinh viên thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/sale-orders
        [HttpGet("sale-orders")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllSaleOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(so => so.Status == status);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var saleOrders = await query
                    .OrderByDescending(so => so.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(so => new
                    {
                        so.SaleOrderId,
                        so.Status,
                        so.SalePrice,
                        Commission = Math.Round(so.SalePrice * 0.05m, 2),
                        so.CreatedAt,
                        so.UpdatedAt,
                        ProductName = so.Product.ProductName,
                        ProductImage = (!string.IsNullOrEmpty(so.Product.ImageUrl) && so.Product.ImageUrl.StartsWith("/uploads/"))
                            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{so.Product.ImageUrl}"
                            : so.Product.ImageUrl,
                        BuyerName = so.Buyer.FullName,
                        BuyerEmail = so.Buyer.Email,
                        PreviousOwnerId = so.PreviousOwnerId,
                        CurrentOwnerId = so.Product.OwnerId,
                        so.Notes
                    })
                    .ToListAsync();

                var result = new
                {
                    items = saleOrders,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách đơn mua thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sale orders");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/payments
        [HttpGet("payments")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? paymentType = null,
            [FromQuery] string? paymentStatus = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Payments
                    .Include(p => p.Payer)
                    .Include(p => p.Rental)
                    .Include(p => p.SaleOrder)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(paymentType))
                {
                    query = query.Where(p => p.PaymentType == paymentType);
                }

                if (!string.IsNullOrWhiteSpace(paymentStatus))
                {
                    query = query.Where(p => p.PaymentStatus == paymentStatus);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var payments = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.PaymentId,
                        p.Amount,
                        p.PaymentType,
                        p.PaymentMethod,
                        p.PaymentStatus,
                        p.TransactionId,
                        p.PaymentDate,
                        p.CreatedAt,
                        PayerName = p.Payer.FullName,
                        PayerEmail = p.Payer.Email,
                        RentalId = p.RentalId,
                        SaleOrderId = p.SaleOrderId,
                        p.Notes
                    })
                    .ToListAsync();

                var result = new
                {
                    items = payments,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách thanh toán thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/transactions
        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? type = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Transactions
                    .Include(t => t.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(t => t.Type == type);
                }

                if (userId.HasValue)
                {
                    query = query.Where(t => t.UserId == userId.Value);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var transactions = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.UserId,
                        UserName = t.User != null ? t.User.FullName : "N/A",
                        UserEmail = t.User != null ? t.User.Email : "N/A",
                        t.Type,
                        t.Amount,
                        t.ReferenceId,
                        t.Description,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var result = new
                {
                    items = transactions,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách giao dịch thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/categories
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        c.IconUrl,
                        c.SortOrder,
                        c.IsActive,
                        ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                    })
                    .ToListAsync();

                return Ok(ApiResponse<object>.SuccessResponse(categories, "Lấy danh sách danh mục thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/reviews
        [HttpGet("reviews")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllReviews(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? productId = null,
            [FromQuery] int? rating = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Reviews
                    .Include(r => r.Product)
                    .Include(r => r.User)
                    .AsQueryable();

                if (productId.HasValue)
                {
                    query = query.Where(r => r.ProductId == productId.Value);
                }

                if (rating.HasValue)
                {
                    query = query.Where(r => r.Rating == rating.Value);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.ReviewId,
                        r.ProductId,
                        ProductName = r.Product.ProductName,
                        r.UserId,
                        UserName = r.User.FullName,
                        UserEmail = r.User.Email,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt
                    })
                    .ToListAsync();

                var result = new
                {
                    items = reviews,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách đánh giá thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetStatistics()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var verifiedUsers = await _context.Users.CountAsync(u => u.IsVerified);
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                
                var totalStudents = await _context.Students.CountAsync();
                var activeStudents = await _context.Students.CountAsync(s => s.Status == "Active");
                
                var totalProducts = await _context.Products.CountAsync();
                var availableProducts = await _context.Products.CountAsync(p => p.IsAvailable);
                var rentProducts = await _context.Products.CountAsync(p => p.ProductType == "Rent" || p.ProductType == "Both");
                var saleProducts = await _context.Products.CountAsync(p => p.ProductType == "Sale" || p.ProductType == "Both");
                
                var totalRentals = await _context.Rentals.CountAsync();
                var pendingRentals = await _context.Rentals.CountAsync(r => r.Status == "Pending");
                var confirmedRentals = await _context.Rentals.CountAsync(r => r.Status == "Confirmed");
                var inProgressRentals = await _context.Rentals.CountAsync(r => r.Status == "InProgress");
                var completedRentals = await _context.Rentals.CountAsync(r => r.Status == "Completed");
                var cancelledRentals = await _context.Rentals.CountAsync(r => r.Status == "Cancelled");
                
                var totalSaleOrders = await _context.SaleOrders.CountAsync();
                var pendingSaleOrders = await _context.SaleOrders.CountAsync(so => so.Status == "Pending");
                var completedSaleOrders = await _context.SaleOrders.CountAsync(so => so.Status == "Completed");
                
                var totalPayments = await _context.Payments.CountAsync();
                var completedPayments = await _context.Payments.CountAsync(p => p.PaymentStatus == "Completed");
                var pendingPayments = await _context.Payments.CountAsync(p => p.PaymentStatus == "Pending");
                
                var totalRevenue = await _context.Rentals
                    .Where(r => r.Status == "Completed")
                    .SumAsync(r => r.TotalPrice);
                
                var totalSaleRevenue = await _context.SaleOrders
                    .Where(so => so.Status == "Completed")
                    .SumAsync(so => so.SalePrice);
                
                var totalCommission = await _context.Transactions
                    .Where(t => t.Type == "Commission")
                    .SumAsync(t => t.Amount);
                
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                var adminBalance = adminUser?.Balance ?? 0;

                var statistics = new
                {
                    users = new
                    {
                        total = totalUsers,
                        verified = verifiedUsers,
                        active = activeUsers,
                        inactive = totalUsers - activeUsers
                    },
                    students = new
                    {
                        total = totalStudents,
                        active = activeStudents,
                        withAccount = await _context.Users.CountAsync(u => u.StudentId != null)
                    },
                    products = new
                    {
                        total = totalProducts,
                        available = availableProducts,
                        forRent = rentProducts,
                        forSale = saleProducts
                    },
                    rentals = new
                    {
                        total = totalRentals,
                        pending = pendingRentals,
                        confirmed = confirmedRentals,
                        inProgress = inProgressRentals,
                        completed = completedRentals,
                        cancelled = cancelledRentals
                    },
                    saleOrders = new
                    {
                        total = totalSaleOrders,
                        pending = pendingSaleOrders,
                        completed = completedSaleOrders
                    },
                    payments = new
                    {
                        total = totalPayments,
                        completed = completedPayments,
                        pending = pendingPayments
                    },
                    revenue = new
                    {
                        totalRentalRevenue = totalRevenue,
                        totalSaleRevenue = totalSaleRevenue,
                        totalCommission = totalCommission,
                        adminBalance = adminBalance
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(statistics, "Lấy thống kê thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserDetail(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Products)
                    .Include(u => u.RentalsAsRenter)
                    .Where(u => u.UserId == id)
                    .Select(u => new
                    {
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        u.AvatarUrl,
                        u.Address,
                        u.Role,
                        u.UserType,
                        u.Balance,
                        u.IsActive,
                        u.IsVerified,
                        u.LastLoginAt,
                        u.CreatedAt,
                        Student = u.Student != null ? new
                        {
                            u.Student.StudentId,
                            u.Student.StudentCode,
                            u.Student.FullName,
                            u.Student.Email,
                            u.Student.Major,
                            u.Student.Faculty,
                            ClassName = u.Student.Class,
                            u.Student.AcademicYear,
                            u.Student.Status
                        } : null,
                        ProductCount = u.Products.Count,
                        RentalCount = u.RentalsAsRenter.Count,
                        TotalSpent = _context.Payments
                            .Where(p => p.PayerId == u.UserId && p.PaymentStatus == "Completed")
                            .Sum(p => p.Amount),
                        TotalEarned = _context.Rentals
                            .Where(r => r.Product.OwnerId == u.UserId && r.Status == "Completed")
                            .Sum(r => r.TotalPrice)
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(ApiResponse<object>.ErrorResponse("Người dùng không tồn tại"));

                return Ok(ApiResponse<object>.SuccessResponse(user, "Lấy thông tin người dùng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user detail");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Admin/pending-products
        [HttpGet("pending-products")]
        public async Task<ActionResult<ApiResponse<object>>> GetPendingProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;

                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => !p.IsApproved);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.PricePerDay,
                        p.PricePerHour,
                        p.PricePerWeek,
                        p.PricePerMonth,
                        p.Deposit,
                        p.ProductType,
                        ImageUrl = (!string.IsNullOrEmpty(p.ImageUrl) && p.ImageUrl.StartsWith("/uploads/"))
                            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{p.ImageUrl}"
                            : p.ImageUrl,
                        p.Location,
                        p.CreatedAt,
                        OwnerName = p.Owner != null ? p.Owner.FullName : "N/A",
                        OwnerEmail = p.Owner != null ? p.Owner.Email : "N/A",
                        CategoryName = p.Category != null ? p.Category.CategoryName : "N/A"
                    })
                    .ToListAsync();

                var result = new
                {
                    items = products,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "Lấy danh sách sản phẩm chờ duyệt thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending products");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Admin/approve-product/5
        [HttpPost("approve-product/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> ApproveProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Sản phẩm không tồn tại"));
                }

                product.IsApproved = true;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, $"Đã duyệt sản phẩm '{product.ProductName}' thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving product");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Admin/approve-all-products
        [HttpPost("approve-all-products")]
        public async Task<ActionResult<ApiResponse<bool>>> ApproveAllProducts()
        {
            try
            {
                var pendingProducts = await _context.Products.Where(p => !p.IsApproved).ToListAsync();
                if (pendingProducts.Count == 0)
                {
                    return Ok(ApiResponse<bool>.SuccessResponse(true, "Không có sản phẩm nào cần duyệt"));
                }

                foreach (var product in pendingProducts)
                {
                    product.IsApproved = true;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, $"Đã duyệt hàng loạt {pendingProducts.Count} sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving all products");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }

    public class EditProductAdminRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }
        public decimal Deposit { get; set; }
        public int Quantity { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}
