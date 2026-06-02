using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        // GET: api/Notification
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Notification>>>> GetNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<List<Notification>>.ErrorResponse("Không xác định được người dùng"));
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId.Value)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50) // limit to latest 50 notifications
                    .ToListAsync();

                return Ok(ApiResponse<List<Notification>>.SuccessResponse(notifications, "Lấy danh sách thông báo thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Notification>>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Notification/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<int>.ErrorResponse("Không xác định được người dùng"));
                }

                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == userId.Value && !n.IsRead)
                    .CountAsync();

                return Ok(ApiResponse<int>.SuccessResponse(unreadCount, "Lấy số lượng thông báo chưa đọc thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<int>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // PUT: api/Notification/mark-read/{id}
        [HttpPut("mark-read/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Không xác định được người dùng"));
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

                if (notification == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy thông báo"));
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Đánh dấu thông báo đã đọc thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // PUT: api/Notification/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Không xác định được người dùng"));
                }

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId.Value && !n.IsRead)
                    .ToListAsync();

                foreach (var notif in unreadNotifications)
                {
                    notif.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Đánh dấu tất cả thông báo đã đọc thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // POST: api/Notification/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin")] // ✅ BẢO VỆ CHẶN SPOOFING: Chỉ Admin mới được tự tạo thông báo thủ công
        public async Task<ActionResult<ApiResponse<Notification>>> CreateNotification([FromBody] Notification model)
        {
            try
            {
                model.CreatedAt = DateTime.Now;
                model.IsRead = false;

                _context.Notifications.Add(model);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Notification>.SuccessResponse(model, "Tạo thông báo thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Notification>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}
