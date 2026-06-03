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
    public class ComplaintController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComplaintController(AppDbContext context)
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

        // POST: api/Complaint
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Complaint>>> CreateComplaint([FromBody] CreateComplaintRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<Complaint>.ErrorResponse("Bạn cần đăng nhập."));

            try
            {
                var rental = await _context.Rentals
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RentalId == request.RentalId);

                if (rental == null)
                    return NotFound(ApiResponse<Complaint>.ErrorResponse("Không tìm thấy đơn thuê để khiếu nại."));

                // Phải là người thuê hoặc người cho thuê mới được khiếu nại
                if (rental.RenterId != userId.Value && rental.Product.OwnerId != userId.Value)
                {
                    return Forbid("Bạn không có quyền khiếu nại cho đơn thuê này.");
                }

                // Kiểm tra điều kiện khiếu nại
                if (userId.Value == rental.RenterId)
                {
                    if (rental.Status != "InProgress" && rental.Status != "Active")
                        return BadRequest(ApiResponse<Complaint>.ErrorResponse("Người thuê chỉ có thể khiếu nại sau khi chủ đồ đã giao hàng và trước khi bạn bấm trả hàng."));
                }
                else if (userId.Value == rental.Product.OwnerId)
                {
                    if (rental.Status != "Active" && rental.Status != "Returned")
                        return BadRequest(ApiResponse<Complaint>.ErrorResponse("Người cho thuê chỉ có thể gửi khiếu nại trong khoảng thời gian người thuê đang sử dụng đồ (Active) đến lúc người thuê báo đã trả đồ (Returned). Không được khiếu nại sau khi đơn thuê đã hoàn thành hoặc trước khi khách nhận đồ."));
                }

                // ✅ BUG #4 FIX: Chặn spam khiếu nại — mỗi người chỉ được 1 khiếu nại đang xử lý/đơn
                var existingComplaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.RentalId == request.RentalId
                                           && c.UserId == userId.Value
                                           && (c.Status == "Pending" || c.Status == "Resolving"));
                if (existingComplaint != null)
                    return BadRequest(ApiResponse<Complaint>.ErrorResponse(
                        "Bạn đã có khiếu nại đang được xử lý cho đơn thuê này. Vui lòng chờ Admin giải quyết trước."));

                var complaint = new Complaint
                {
                    RentalId = request.RentalId,
                    UserId = userId.Value,
                    Reason = request.Reason,
                    Title = request.Title,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Complaint>.SuccessResponse(complaint, "Đã gửi khiếu nại thành công. Hệ thống sẽ xử lý sớm nhất."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Complaint>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Complaint/user
        [HttpGet("user")]
        public async Task<ActionResult<ApiResponse<List<Complaint>>>> GetUserComplaints()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<List<Complaint>>.ErrorResponse("Bạn cần đăng nhập."));

            try
            {
                var complaints = await _context.Complaints
                    .Include(c => c.Rental)
                        .ThenInclude(r => r!.Product)
                    .Where(c => c.UserId == userId.Value)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Complaint>>.SuccessResponse(complaints, "Lấy danh sách khiếu nại thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Complaint>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
        // GET: api/Complaint/admin
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<List<Complaint>>>> GetAllComplaints()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return Forbid("Chỉ admin mới có quyền xem.");

            try
            {
                var complaints = await _context.Complaints
                    .Include(c => c.User)
                    .Include(c => c.Rental)
                        .ThenInclude(r => r!.Product)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Complaint>>.SuccessResponse(complaints, "Lấy danh sách khiếu nại thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Complaint>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Complaint/admin/{id}
        [HttpPut("admin/{id}")]
        public async Task<ActionResult<ApiResponse<Complaint>>> UpdateComplaintStatus(int id, [FromBody] string status)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return Forbid("Chỉ admin mới có quyền.");

            // ✅ BUG #6 FIX: Validate giá trị status
            var validStatuses = new[] { "Pending", "Resolving", "Resolved", "Rejected" };
            if (!validStatuses.Contains(status))
                return BadRequest(ApiResponse<Complaint>.ErrorResponse(
                    $"Trạng thái '{status}' không hợp lệ. Cho phép: {string.Join(", ", validStatuses)}"));

            try
            {
                var complaint = await _context.Complaints.FindAsync(id);
                if (complaint == null) return NotFound(ApiResponse<Complaint>.ErrorResponse("Không tìm thấy khiếu nại."));

                if (complaint.Status == "Resolved" || complaint.Status == "Rejected")
                    return BadRequest(ApiResponse<Complaint>.ErrorResponse("Không thể thay đổi trạng thái khiếu nại đã kết thúc."));

                complaint.Status = status;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Complaint>.SuccessResponse(complaint, "Cập nhật trạng thái thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Complaint>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
        // POST: api/Complaint/admin/{id}/resolve
        [HttpPost("admin/{id}/resolve")]
        public async Task<ActionResult<ApiResponse<Complaint>>> ResolveComplaint(int id, [FromBody] ResolveComplaintRequest request)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return Forbid("Chỉ admin mới có quyền.");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var complaint = await _context.Complaints
                    .Include(c => c.Rental)
                        .ThenInclude(r => r!.Product)
                    .FirstOrDefaultAsync(c => c.ComplaintId == id);

                if (complaint == null) return NotFound(ApiResponse<Complaint>.ErrorResponse($"Không tìm thấy khiếu nại với ID = {id}."));
                if (complaint.Status == "Resolved") return BadRequest(ApiResponse<Complaint>.ErrorResponse("Khiếu nại này đã được giải quyết rồi."));

                var rental = complaint.Rental!;

                // ✅ HỆ THỐNG KÝ QUỸ TRUNG TÂM (ESCROW):
                // Do Admin đã nắm giữ toàn bộ Tiền thuê + Tiền cọc trong ví ngay từ lúc SV đặt thuê,
                // nên việc đền bù/hoàn trả chỉ đơn giản là trích tiền từ ví Admin ra để chuyển cho các bên tương ứng.
                
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser == null)
                    return StatusCode(500, ApiResponse<Complaint>.ErrorResponse("Không tìm thấy tài khoản Admin."));

                decimal netPayout = request.OwnerCompensation + request.RenterRefund;

                // Kiểm tra ví Admin có đủ tiền mặt thực tế để chi trả không
                if (netPayout > 0 && adminUser.Balance < netPayout)
                    return BadRequest(ApiResponse<Complaint>.ErrorResponse(
                        $"Ví Admin không đủ số dư để chi trả đền bù. " +
                        $"Cần: {netPayout:N0}đ, số dư ví Admin hiện tại: {adminUser.Balance:N0}đ."));

                // Trừ tiền từ ví Admin
                if (netPayout != 0)
                {
                    adminUser.Balance -= netPayout;
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = adminUser.UserId,
                        Type = "EscrowRelease",
                        Amount = -netPayout,
                        ReferenceId = rental.RentalId,
                        Description = $"Admin giải ngân từ ví ký quỹ xử lý khiếu nại #{complaint.ComplaintId} (Chi chủ: -{request.OwnerCompensation:N0}đ, Hoàn khách: -{request.RenterRefund:N0}đ)",
                        CreatedAt = DateTime.Now
                    });
                }

                // 1. Cộng tiền bồi thường cho Chủ đồ
                if (request.OwnerCompensation != 0)
                {
                    var ownerUser = await _context.Users.FindAsync(rental.Product.OwnerId);
                    if (ownerUser != null)
                    {
                        ownerUser.Balance += request.OwnerCompensation;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = ownerUser.UserId,
                            Type = "Refund",
                            Amount = request.OwnerCompensation,
                            ReferenceId = rental.RentalId,
                            Description = request.OwnerCompensation > 0
                                ? $"Đền bù khiếu nại #{complaint.ComplaintId} (+{request.OwnerCompensation:N0}đ)"
                                : $"Thu hồi do xử lý khiếu nại #{complaint.ComplaintId} ({request.OwnerCompensation:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // 2. Cộng tiền hoàn trả cho Khách thuê
                if (request.RenterRefund != 0)
                {
                    var renterUser = await _context.Users.FindAsync(rental.RenterId);
                    if (renterUser != null)
                    {
                        renterUser.Balance += request.RenterRefund;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = renterUser.UserId,
                            Type = "Refund",
                            Amount = request.RenterRefund,
                            ReferenceId = rental.RentalId,
                            Description = request.RenterRefund > 0
                                ? $"Hoàn tiền khiếu nại #{complaint.ComplaintId} (+{request.RenterRefund:N0}đ)"
                                : $"Phạt/Thu hồi do xử lý khiếu nại #{complaint.ComplaintId} ({request.RenterRefund:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // 3. Update Rental & Complaint Status
                rental.Status = "Completed"; // Mark rental as fully done
                rental.DepositRefunded = true; // Mark deposit as handled
                rental.UpdatedAt = DateTime.Now;

                complaint.Status = "Resolved";
                complaint.Description += $"\n\n[ADMIN RESOLUTION]: {request.AdminNotes} | Bồi thường chủ: {request.OwnerCompensation:N0}đ | Hoàn người thuê: {request.RenterRefund:N0}đ";
                
                // Notifications
                _context.Notifications.Add(new Notification
                {
                    UserId = rental.RenterId,
                    Title = "Khiếu nại đã được giải quyết",
                    Body = $"Khiếu nại cho đơn #{rental.RentalId} đã được Admin xử lý. Bạn được hoàn lại {request.RenterRefund:N0}đ.",
                    Type = "System",
                    ReferenceId = complaint.ComplaintId,
                    CreatedAt = DateTime.Now
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = rental.Product.OwnerId,
                    Title = "Khiếu nại đã được giải quyết",
                    Body = $"Khiếu nại cho đơn #{rental.RentalId} đã được Admin xử lý. Bạn nhận bồi thường {request.OwnerCompensation:N0}đ.",
                    Type = "System",
                    ReferenceId = complaint.ComplaintId,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(ApiResponse<Complaint>.SuccessResponse(complaint, "Đã giải quyết khiếu nại và chuyển tiền thành công."));
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, ApiResponse<Complaint>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
