using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.DTOs.Request;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RentalController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const decimal COMMISSION_RATE = 0.05m; // 5% hoa hồng cho admin

        public RentalController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // GET: api/Rental
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetAllRentals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var currentUserId = GetCurrentUserId();
                var role = GetCurrentUserRole();

                // ✅ BUG #8 FIX: Chỉ Admin mới xem được tất cả đơn thuê
                // User thường chỉ xem đơn của mình (thuê hoặc cho thuê)
                var query = _context.Rentals
                    .Include(r => r.Product)
                    .Include(r => r.Renter)
                    .Include(r => r.Payments)
                    .AsQueryable();

                if (role != "Admin" && currentUserId.HasValue)
                    query = query.Where(r => r.RenterId == currentUserId.Value
                                         || r.Product.OwnerId == currentUserId.Value);

                query = query.OrderByDescending(r => r.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var rentals = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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

                return Ok(ApiResponse<object>.SuccessResponse(result,
                    $"Lấy danh sách đơn thuê thành công (Trang {page}/{totalPages})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Rental/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Rental>>> GetRental(int id)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .Include(r => r.Renter)
                    .Include(r => r.Payments)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                return Ok(ApiResponse<Rental>.SuccessResponse(rental, "Lấy thông tin đơn thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Rental/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<Rental>>>> GetRentalsByUser(int userId)
        {
            try
            {
                var rentals = await _context.Rentals
                    .Include(r => r.Product)
                    .Include(r => r.Payments)
                    .Where(r => r.RenterId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Rental>>.SuccessResponse(rentals, "Lấy lịch sử thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Rental>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Rental/owner/5
        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<ApiResponse<List<Rental>>>> GetRentalsByOwner(int ownerId)
        {
            try
            {
                var rentals = await _context.Rentals
                    .Include(r => r.Renter)
                    .Include(r => r.Product)
                    .Where(r => r.Product.OwnerId == ownerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Rental>>.SuccessResponse(rentals, "Lấy danh sách đơn thuê của chủ sở hữu thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Rental>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Rental/product/{productId}/booked-dates
        [HttpGet("product/{productId}/booked-dates")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> GetBookedDates(int productId)
        {
            try
            {
                var rentals = await _context.Rentals
                    .Where(r => r.ProductId == productId && r.Status != "Cancelled" && r.Status != "Completed")
                    .Select(r => new { startDate = r.StartDate, endDate = r.EndDate })
                    .ToListAsync();

                return Ok(ApiResponse<object>.SuccessResponse(rentals, "Lấy lịch đặt sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Rental — Đặt thuê: trừ tiền thuê + tiền cọc ngay lập tức
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Rental>>> CreateRental([FromBody] CreateRentalRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var product = await _context.Products
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

                if (product == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy sản phẩm"));

                if (product.OwnerId == currentUserId.Value)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Bạn không thể thuê sản phẩm của chính mình"));

                if (!product.IsAvailable || product.Quantity <= 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Sản phẩm hiện không có sẵn"));

                // ✅ FIX EXPLOIT: Ngăn chặn hack qua Postman bằng cách truyền số âm
                if (request.Quantity <= 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Số lượng thuê phải lớn hơn 0"));

                if (request.DepositAmount < 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Tiền cọc không được là số âm"));

                if (request.DepositAmount != product.Deposit)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Sai lệch tiền cọc. Yêu cầu: {product.Deposit:N0}đ, Nhận được: {request.DepositAmount:N0}đ"));

                if (request.EndDate <= request.StartDate)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Ngày trả đồ phải sau ngày nhận đồ"));

                // ✅ BUG #1 FIX: Server tự tính giá — không tin TotalPrice từ client
                var duration = request.EndDate - request.StartDate;
                decimal calculatedPrice = request.RentalUnit switch
                {
                    "Hour"  => (product.PricePerHour  ?? 0m) * (decimal)Math.Ceiling(duration.TotalHours)  * request.Quantity,
                    "Day"   => product.PricePerDay            * (decimal)Math.Ceiling(duration.TotalDays)   * request.Quantity,
                    "Week"  => (product.PricePerWeek  ?? 0m) * (decimal)Math.Ceiling(duration.TotalDays / 7.0) * request.Quantity,
                    "Month" => (product.PricePerMonth ?? 0m) * (decimal)Math.Ceiling(duration.TotalDays / 30.0) * request.Quantity,
                    _ => 0m
                };
                if (calculatedPrice <= 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Sản phẩm không có giá thuê theo đơn vị '{request.RentalUnit}'."));
                // Cho phép sai lệch tối đa 1đ do làm tròn
                if (Math.Abs(request.TotalPrice - calculatedPrice) > 1)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Giá thuê không hợp lệ. Giá đúng: {calculatedPrice:N0}đ, giá nhận: {request.TotalPrice:N0}đ. Vui lòng tải lại trang."));

                // Dùng giá server tính — không dùng giá client gửi
                var serverTotalPrice = calculatedPrice;

                // Kiểm tra xung đột thời gian
                var conflictCount = await _context.Rentals
                    .Where(r => r.ProductId == request.ProductId
                        && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Active" || r.Status == "InProgress")
                        && r.StartDate < request.EndDate
                        && r.EndDate > request.StartDate)
                    .CountAsync();

                var availableQuantity = product.Quantity - conflictCount;
                if (availableQuantity <= 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Sản phẩm đã hết chỗ trong khoảng thời gian này."));

                // ✅ BUG #2 FIX: Kiểm tra trạng thái tài khoản
                var renter = await _context.Users.FindAsync(currentUserId.Value);
                if (renter == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Người dùng không tồn tại"));

                if (!renter.IsActive)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Admin."));

                if (!renter.IsVerified)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse("Tài khoản chưa xác thực email trường. Vui lòng xác thực OTP trước khi đặt thuê."));

                decimal totalRequired = serverTotalPrice + request.DepositAmount;
                if (renter.Balance < totalRequired)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Số dư không đủ. Cần {totalRequired:N0}đ (Giá thuê: {serverTotalPrice:N0}đ + Tiền cọc: {request.DepositAmount:N0}đ). Số dư hiện tại: {renter.Balance:N0}đ."));

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ✅ Trừ tiền ngay khi đặt thuê (tiền thuê + tiền cọc)
                    renter.Balance -= totalRequired;

                    var rental = new Rental
                    {
                        ProductId = request.ProductId,
                        RenterId = currentUserId.Value,
                        Quantity = request.Quantity,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        RentalUnit = request.RentalUnit,
                        TotalPrice = serverTotalPrice, // ✅ Dùng giá server tính
                        DepositAmount = request.DepositAmount,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };

                    _context.Rentals.Add(rental);
                    await _context.SaveChangesAsync();

                    // ✅ Cộng tiền đặt thuê (tiền thuê + tiền cọc) vào ví Admin (Hệ thống ký quỹ Escrow nắm giữ)
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser != null)
                    {
                        adminUser.Balance += totalRequired;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = adminUser.UserId,
                            Type = "EscrowHold",
                            Amount = totalRequired,
                            ReferenceId = rental.RentalId,
                            Description = $"Nhận ký quỹ đơn thuê #{rental.RentalId} từ SV {renter.FullName} (Thuê: +{serverTotalPrice:N0}đ + Cọc: +{request.DepositAmount:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // Ghi giao dịch trừ tiền người thuê
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = renter.UserId,
                        Type = "Payment",
                        Amount = -totalRequired,
                        ReferenceId = rental.RentalId,
                        Description = $"Thanh toán ký quỹ đơn thuê #{rental.RentalId}: {product.ProductName} (Thuê: -{request.TotalPrice:N0}đ + Cọc: -{request.DepositAmount:N0}đ)",
                        CreatedAt = DateTime.Now
                    });

                    // ✅ Tạo thông báo cho chủ đồ
                    _context.Notifications.Add(new Notification
                    {
                        UserId = product.OwnerId,
                        Title = "Yêu cầu thuê mới",
                        Body = $"Sinh viên {renter.FullName} muốn thuê sản phẩm '{product.ProductName}' của bạn.",
                        Type = "RentalRequest",
                        ReferenceId = rental.RentalId,
                        ReferenceType = "Rental",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return CreatedAtAction(nameof(GetRental), new { id = rental.RentalId },
                        ApiResponse<Rental>.SuccessResponse(rental,
                            $"Đặt thuê thành công! Đã trừ {totalRequired:N0}đ từ ví (Tiền thuê: {request.TotalPrice:N0}đ + Tiền cọc: {request.DepositAmount:N0}đ)."));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/confirm — Người cho thuê xác nhận đơn
        [HttpPut("{id}/confirm")]
        public async Task<ActionResult<ApiResponse<Rental>>> ConfirmRental(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.Product.OwnerId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền xác nhận đơn này"));

                if (rental.Status != "Pending")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Không thể xác nhận đơn ở trạng thái: {rental.Status}"));

                rental.Status = "Confirmed";
                rental.UpdatedAt = DateTime.Now;

                // ✅ Tạo thông báo cho người thuê
                _context.Notifications.Add(new Notification
                {
                    UserId = rental.RenterId,
                    Title = "Đơn thuê được chấp nhận",
                    Body = $"Yêu cầu thuê '{rental.Product.ProductName}' đã được chủ đồ chấp nhận.",
                    Type = "RentalConfirmed",
                    ReferenceId = rental.RentalId,
                    ReferenceType = "Rental",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Rental>.SuccessResponse(rental, "Xác nhận đơn thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/deliver
        // ✅ NGƯỜI CHO THUÊ XÁC NHẬN GIAO HÀNG:
        //    - Trạng thái → "Active" (đang cho thuê)
        //    - Tiền thuê (trừ 5% hoa hồng) → cộng vào ví chủ sản phẩm
        //    - 5% hoa hồng → cộng vào ví Admin
        //    - Tiền cọc vẫn giữ trong hệ thống (chờ trả đồ mới hoàn)
        [HttpPut("{id}/deliver")]
        public async Task<ActionResult<ApiResponse<Rental>>> ConfirmDelivery(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.Product.OwnerId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền xác nhận giao hàng cho đơn này"));

                if (rental.Status != "Confirmed")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Chỉ có thể xác nhận giao hàng khi đơn ở trạng thái 'Confirmed'. Trạng thái hiện tại: {rental.Status}"));

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {


                    // Tiền cọc sẽ được giữ lại trong hệ thống cho đến khi hoàn thành đơn thuê (CompleteRental)
                    
                    // ✅ 4. Cập nhật trạng thái → InProgress (Đã giao hàng, chờ người thuê xác nhận)
                    rental.Status = "InProgress";
                    rental.UpdatedAt = DateTime.Now;

                    // ✅ Tạo thông báo cho người thuê
                    _context.Notifications.Add(new Notification
                    {
                        UserId = rental.RenterId,
                        Title = "Đồ thuê đã được giao",
                        Body = $"Chủ đồ đã xác nhận bàn giao sản phẩm '{rental.Product.ProductName}' cho bạn.",
                        Type = "System",
                        ReferenceId = rental.RentalId,
                        ReferenceType = "Rental",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(ApiResponse<Rental>.SuccessResponse(rental,
                        "Xác nhận giao hàng thành công! Đang chờ người thuê xác nhận nhận hàng. (Tiền thuê sẽ được chuyển vào ví khi khách xác nhận nhận đồ)"));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/receive
        // ✅ NGƯỜI THUÊ XÁC NHẬN ĐÃ LẤY ĐỒ:
        //    - Trạng thái → "Active"
        [HttpPut("{id}/receive")]
        public async Task<ActionResult<ApiResponse<Rental>>> ReceiveRental(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.RenterId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền xác nhận cho đơn này"));

                if (rental.Status != "InProgress")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Chỉ có thể xác nhận nhận hàng khi đơn ở trạng thái 'InProgress'. Trạng thái hiện tại: {rental.Status}"));

                rental.Status = "Active";
                rental.UpdatedAt = DateTime.Now;

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    decimal commission = Math.Round(rental.TotalPrice * COMMISSION_RATE, 2);
                    decimal ownerPayout = rental.TotalPrice - commission;

                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser == null)
                        return StatusCode(500, ApiResponse<Rental>.ErrorResponse("Không tìm thấy tài khoản Admin."));

                    // ✅ 1. Cộng tiền thuê (đã trừ 5%) vào ví người cho thuê
                    var ownerUser = await _context.Users.FindAsync(rental.Product.OwnerId);
                    if (ownerUser != null)
                    {
                        ownerUser.Balance += ownerPayout;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = ownerUser.UserId,
                            Type = "Payment",
                            Amount = ownerPayout,
                            ReferenceId = rental.RentalId,
                            Description = $"Nhận tiền thuê đơn #{rental.RentalId} do khách đã xác nhận nhận đồ (Giá: {rental.TotalPrice:N0}đ - Hoa hồng: -{commission:N0}đ = +{ownerPayout:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // ✅ 2. Giải ngân từ ví Admin cho chủ sản phẩm (giảm trừ tiền trong ví Admin đang nắm giữ)
                    // Vì Admin đã cầm 100% tiền thuê lúc đặt, nên khi giải ngân Admin sẽ chuyển ownerPayout (95%) đi, tự giữ lại 5% commission.
                    adminUser.Balance -= ownerPayout;
                    var productNameShort = rental.Product.ProductName.Length > 50 
                        ? rental.Product.ProductName.Substring(0, 47) + "..." 
                        : rental.Product.ProductName;
                        
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = adminUser.UserId,
                        Type = "EscrowRelease",
                        Amount = -ownerPayout,
                        ReferenceId = rental.RentalId,
                        Description = $"Giải ngân ký quỹ đơn thuê #{rental.RentalId} cho chủ đồ (SP: {productNameShort}) (-{ownerPayout:N0}đ). Tự động thu 5% hoa hồng: +{commission:N0}đ",
                        CreatedAt = DateTime.Now
                    });

                    // Tạo thông báo cho chủ đồ
                    _context.Notifications.Add(new Notification
                    {
                        UserId = rental.Product.OwnerId,
                        Title = "Người thuê đã nhận đồ & Tiền vào ví",
                        Body = $"Người thuê đã nhận được sản phẩm '{rental.Product.ProductName}'. Bạn được cộng {ownerPayout:N0}đ vào ví.",
                        Type = "System",
                        ReferenceId = rental.RentalId,
                        ReferenceType = "Rental",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(ApiResponse<Rental>.SuccessResponse(rental, "Xác nhận nhận hàng thành công! Chủ đồ đã nhận được tiền thuê. Vui lòng giữ gìn sản phẩm."));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/return
        // ✅ NGƯỜI THUÊ XÁC NHẬN ĐÃ TRẢ ĐỒ:
        //    - Trạng thái → "Returned"
        [HttpPut("{id}/return")]
        public async Task<ActionResult<ApiResponse<Rental>>> ReturnRental(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.RenterId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền xác nhận cho đơn này"));

                if (rental.Status != "Active")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Chỉ có thể xác nhận trả hàng khi đơn đang sử dụng ('Active'). Trạng thái hiện tại: {rental.Status}"));

                rental.Status = "Returned";
                rental.UpdatedAt = DateTime.Now;

                // Tạo thông báo cho chủ đồ
                _context.Notifications.Add(new Notification
                {
                    UserId = rental.Product.OwnerId,
                    Title = "Người thuê đã trả đồ",
                    Body = $"Người thuê báo cáo đã trả lại sản phẩm '{rental.Product.ProductName}'. Vui lòng kiểm tra và hoàn thành đơn.",
                    Type = "System",
                    ReferenceId = rental.RentalId,
                    ReferenceType = "Rental",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Rental>.SuccessResponse(rental, "Đã gửi thông báo trả đồ thành công! Chờ chủ đồ kiểm tra và xác nhận."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/complete
        // ✅ NGƯỜI CHO THUÊ XÁC NHẬN ĐÃ NHẬN LẠI ĐỒ:
        //    - Trạng thái → "Completed"
        //    - Tiền cọc → hoàn về ví người thuê
        [HttpPut("{id}/complete")]
        public async Task<ActionResult<ApiResponse<Rental>>> CompleteRental(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.Product.OwnerId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền hoàn thành đơn này"));

                // ✅ BUG #3 FIX: Chỉ được complete khi người thuê đã báo trả (Returned)
                // Không cho phép complete từ Active — phải đi qua bước return trước
                if (rental.Status != "Returned")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Chỉ có thể hoàn thành đơn sau khi người thuê đã xác nhận trả hàng ('Returned'). Trạng thái hiện tại: {rental.Status}"));

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ✅ Hoàn trả tiền cọc cho người thuê trích từ ví Admin (do hệ thống Admin đang nắm giữ tiền ký quỹ này)
                    if (rental.DepositAmount > 0 && !rental.DepositRefunded)
                    {
                        var renterUser = await _context.Users.FindAsync(rental.RenterId);
                        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

                        if (renterUser != null && adminUser != null)
                        {
                            // Cộng lại tiền cọc cho người thuê
                            renterUser.Balance += rental.DepositAmount;
                            _context.Transactions.Add(new Transaction
                            {
                                UserId = renterUser.UserId,
                                Type = "Refund",
                                Amount = rental.DepositAmount,
                                ReferenceId = rental.RentalId,
                                Description = $"Hoàn tiền cọc đơn thuê #{rental.RentalId} sau khi trả đồ (+{rental.DepositAmount:N0}đ)",
                                CreatedAt = DateTime.Now
                            });

                            // Trừ tiền cọc từ ví Admin đang giữ
                            adminUser.Balance -= rental.DepositAmount;
                            _context.Transactions.Add(new Transaction
                            {
                                UserId = adminUser.UserId,
                                Type = "EscrowRelease",
                                Amount = -rental.DepositAmount,
                                ReferenceId = rental.RentalId,
                                Description = $"Hoàn trả ký quỹ cọc đơn thuê #{rental.RentalId} cho người thuê (-{rental.DepositAmount:N0}đ)",
                                CreatedAt = DateTime.Now
                            });
                        }
                        rental.DepositRefunded = true;
                    }

                    rental.Status = "Completed";
                    rental.ActualReturnDate = DateTime.Now;
                    rental.UpdatedAt = DateTime.Now;

                    // ✅ Tạo thông báo cho người thuê
                    _context.Notifications.Add(new Notification
                    {
                        UserId = rental.RenterId,
                        Title = "Đơn thuê đã hoàn thành",
                        Body = $"Chủ đồ đã nhận lại sản phẩm '{rental.Product.ProductName}' và xác nhận hoàn thành đơn thuê.",
                        Type = "RentalCompleted",
                        ReferenceId = rental.RentalId,
                        ReferenceType = "Rental",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(ApiResponse<Rental>.SuccessResponse(rental,
                        $"Hoàn thành đơn thuê! Đã hoàn trả {rental.DepositAmount:N0}đ tiền cọc cho người thuê."));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/cancel — Hủy đơn (hoàn toàn bộ tiền)
        [HttpPut("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<Rental>>> CancelRental(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<Rental>.ErrorResponse("Không xác định được người dùng"));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                if (rental.RenterId != currentUserId.Value && rental.Product.OwnerId != currentUserId.Value)
                    return StatusCode(403, ApiResponse<Rental>.ErrorResponse("Bạn không có quyền hủy đơn này"));

                if (rental.Status != "Pending" && rental.Status != "Confirmed")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Không thể hủy đơn ở trạng thái: {rental.Status}"));

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Hoàn toàn bộ tiền thuê + tiền cọc cho người thuê trích từ ví Admin (vì hệ thống Admin đang nắm giữ toàn bộ)
                    decimal totalRefund = rental.TotalPrice + rental.DepositAmount;
                    var renterUser = await _context.Users.FindAsync(rental.RenterId);
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

                    if (renterUser != null && adminUser != null)
                    {
                        // Cộng lại tiền cho người thuê
                        renterUser.Balance += totalRefund;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = renterUser.UserId,
                            Type = "Refund",
                            Amount = totalRefund,
                            ReferenceId = rental.RentalId,
                            Description = $"Hoàn tiền hủy đơn thuê #{rental.RentalId} (Thuê: {rental.TotalPrice:N0}đ + Cọc: {rental.DepositAmount:N0}đ = +{totalRefund:N0}đ)",
                            CreatedAt = DateTime.Now
                        });

                        // Trừ khỏi ví Admin đang giữ
                        adminUser.Balance -= totalRefund;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = adminUser.UserId,
                            Type = "EscrowRelease",
                            Amount = -totalRefund,
                            ReferenceId = rental.RentalId,
                            Description = $"Hoàn trả ký quỹ hủy đơn thuê #{rental.RentalId} cho người thuê (-{totalRefund:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    rental.Status = "Cancelled";
                    rental.UpdatedAt = DateTime.Now;

                    // ✅ Tạo thông báo cho đối phương
                    int targetNotifyUserId = (currentUserId.Value == rental.RenterId) ? rental.Product.OwnerId : rental.RenterId;
                    string notifierRoleName = (currentUserId.Value == rental.RenterId) ? "Người thuê" : "Chủ đồ";

                    _context.Notifications.Add(new Notification
                    {
                        UserId = targetNotifyUserId,
                        Title = "Đơn thuê đã bị hủy",
                        Body = $"{notifierRoleName} đã hủy đơn thuê #{rental.RentalId} cho sản phẩm '{rental.Product.ProductName}'.",
                        Type = "RentalCancelled",
                        ReferenceId = rental.RentalId,
                        ReferenceType = "Rental",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(ApiResponse<Rental>.SuccessResponse(rental,
                        $"Hủy đơn thành công. Đã hoàn {totalRefund:N0}đ vào ví người thuê."));
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Rental/{id}/status — Cập nhật trạng thái tổng quát (Admin)
        // ✅ BUG #7 FIX: Admin không được set Completed/Cancelled qua endpoint này
        //   vì sẽ bỏ qua business logic (hoàn cọc, hoa hồng, notifications)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Rental>>> UpdateRentalStatus(int id, [FromBody] string status)
        {
            try
            {
                // ✅ Chỉ cho phép Admin set các trạng thái ghi chú, không được bypass business logic
                var allowedStatuses = new[] { "Pending", "Confirmed", "InProgress", "Active", "Returned", "Disputed" };
                if (!allowedStatuses.Contains(status))
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Trạng thái '{status}' không được phép qua endpoint này. " +
                        "Dùng /complete để hoàn thành (có hoàn cọc) hoặc /cancel để hủy (có hoàn tiền)."));

                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                var oldStatus = rental.Status;
                rental.Status = status;
                rental.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Rental>.SuccessResponse(rental,
                    $"Admin cập nhật trạng thái đơn #{id}: {oldStatus} → {status}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Rental>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Rental/{id} — Xóa (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteRental(int id)
        {
            try
            {
                var rental = await _context.Rentals.FindAsync(id);
                if (rental == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy đơn thuê"));

                _context.Rentals.Remove(rental);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa đơn thuê thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}