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

                var query = _context.Rentals
                    .Include(r => r.Product)
                    .Include(r => r.Renter)
                    .Include(r => r.Payments)
                    .OrderByDescending(r => r.CreatedAt);

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

                // Kiểm tra xung đột thời gian
                var conflictCount = await _context.Rentals
                    .Where(r => r.ProductId == request.ProductId
                        && (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "Active")
                        && r.StartDate < request.EndDate
                        && r.EndDate > request.StartDate)
                    .CountAsync();

                var availableQuantity = product.Quantity - conflictCount;
                if (availableQuantity <= 0)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Sản phẩm đã hết chỗ trong khoảng thời gian này."));

                // Kiểm tra số dư ví
                var renter = await _context.Users.FindAsync(currentUserId.Value);
                if (renter == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Người dùng không tồn tại"));

                decimal totalRequired = request.TotalPrice + request.DepositAmount;
                if (renter.Balance < totalRequired)
                    return BadRequest(ApiResponse<Rental>.ErrorResponse(
                        $"Số dư không đủ. Cần {totalRequired:N0}đ (Giá thuê: {request.TotalPrice:N0}đ + Tiền cọc: {request.DepositAmount:N0}đ). Số dư hiện tại: {renter.Balance:N0}đ."));

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
                        TotalPrice = request.TotalPrice,
                        DepositAmount = request.DepositAmount,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };

                    _context.Rentals.Add(rental);
                    await _context.SaveChangesAsync();

                    // Ghi giao dịch trừ tiền
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = renter.UserId,
                        Type = "Payment",
                        Amount = -totalRequired,
                        ReferenceId = rental.RentalId,
                        Description = $"Thanh toán ký quỹ đơn thuê #{rental.RentalId}: {product.ProductName} (Thuê: -{request.TotalPrice:N0}đ + Cọc: -{request.DepositAmount:N0}đ)",
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
                    decimal commission = Math.Round(rental.TotalPrice * COMMISSION_RATE, 2);
                    decimal ownerPayout = rental.TotalPrice - commission;

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
                            Description = $"Nhận tiền thuê đơn #{rental.RentalId} sau khi giao hàng (Giá thuê: {rental.TotalPrice:N0}đ - 5% hoa hồng: -{commission:N0}đ = +{ownerPayout:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // ✅ 2. Cộng 5% hoa hồng vào ví Admin
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser != null)
                    {
                        adminUser.Balance += commission;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = adminUser.UserId,
                            Type = "Commission",
                            Amount = commission,
                            ReferenceId = rental.RentalId,
                            Description = $"Hoa hồng 5% đơn thuê #{rental.RentalId} (sản phẩm: {rental.Product.ProductName}) +{commission:N0}đ",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // ✅ 3. Hoàn trả tiền cọc về tài khoản người thuê ngay khi giao hàng
                    if (rental.DepositAmount > 0 && !rental.DepositRefunded)
                    {
                        var renterUser = await _context.Users.FindAsync(rental.RenterId);
                        if (renterUser != null)
                        {
                            renterUser.Balance += rental.DepositAmount;
                            _context.Transactions.Add(new Transaction
                            {
                                UserId = renterUser.UserId,
                                Type = "Refund",
                                Amount = rental.DepositAmount,
                                ReferenceId = rental.RentalId,
                                Description = $"Hoàn tiền cọc đơn thuê #{rental.RentalId} ngay khi người cho thuê giao hàng (+{rental.DepositAmount:N0}đ)",
                                CreatedAt = DateTime.Now
                            });
                        }
                        rental.DepositRefunded = true;
                    }

                    // ✅ 4. Cập nhật trạng thái → Active (InProgress)
                    rental.Status = "Active";
                    rental.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(ApiResponse<Rental>.SuccessResponse(rental,
                        $"Xác nhận giao hàng thành công! Đã nhận {ownerPayout:N0}đ vào ví (sau khi trừ 5% hoa hồng {commission:N0}đ). Tiền cọc {rental.DepositAmount:N0}đ đã được hoàn trả về ví của người thuê."));
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

                if (rental.Status != "Active")
                    return BadRequest(ApiResponse<Rental>.ErrorResponse($"Chỉ có thể hoàn thành đơn ở trạng thái 'Active'. Trạng thái hiện tại: {rental.Status}"));

                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ✅ Hoàn trả tiền cọc cho người thuê
                    if (rental.DepositAmount > 0 && !rental.DepositRefunded)
                    {
                        var renterUser = await _context.Users.FindAsync(rental.RenterId);
                        if (renterUser != null)
                        {
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
                        }
                        rental.DepositRefunded = true;
                    }

                    rental.Status = "Completed";
                    rental.ActualReturnDate = DateTime.Now;
                    rental.UpdatedAt = DateTime.Now;

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
                    // Hoàn toàn bộ tiền thuê + tiền cọc cho người thuê
                    decimal totalRefund = rental.TotalPrice + rental.DepositAmount;
                    var renterUser = await _context.Users.FindAsync(rental.RenterId);
                    if (renterUser != null)
                    {
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
                    }

                    rental.Status = "Cancelled";
                    rental.UpdatedAt = DateTime.Now;

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
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Rental>>> UpdateRentalStatus(int id, [FromBody] string status)
        {
            try
            {
                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                    return NotFound(ApiResponse<Rental>.ErrorResponse("Không tìm thấy đơn thuê"));

                rental.Status = status;
                rental.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Rental>.SuccessResponse(rental, "Cập nhật trạng thái thành công (Admin)"));
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