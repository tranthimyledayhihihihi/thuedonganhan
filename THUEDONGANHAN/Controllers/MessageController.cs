using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Request;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MessageController> _logger;

        public MessageController(AppDbContext context, ILogger<MessageController> logger)
        {
            _context = context;
            _logger = logger;
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

        // POST: api/Message
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Message>>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var senderId = GetCurrentUserId();
                if (senderId == null)
                {
                    return Unauthorized(ApiResponse<Message>.ErrorResponse("Không xác định được người dùng"));
                }

                if (senderId == request.ReceiverId)
                {
                    return BadRequest(ApiResponse<Message>.ErrorResponse("Bạn không thể tự nhắn tin cho chính mình"));
                }

                // Kiểm tra người nhận có tồn tại không
                var receiverExists = await _context.Users.AnyAsync(u => u.UserId == request.ReceiverId && u.IsActive);
                if (!receiverExists)
                {
                    return NotFound(ApiResponse<Message>.ErrorResponse("Không tìm thấy người nhận hoặc tài khoản đã bị khóa"));
                }

                var message = new Message
                {
                    SenderId = senderId.Value,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    ProductId = request.ProductId,
                    RentalId = request.RentalId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Load navigation properties for response
                var dbMsg = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Product)
                    .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

                return Ok(ApiResponse<Message>.SuccessResponse(dbMsg!, "Gửi tin nhắn thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi tin nhắn");
                return StatusCode(500, ApiResponse<Message>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Message/conversation/{otherUserId}
        [HttpGet("conversation/{otherUserId}")]
        public async Task<ActionResult<ApiResponse<List<Message>>>> GetConversation(int otherUserId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<Message>>.ErrorResponse("Không xác định được người dùng"));
                }

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Product)
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // Đánh dấu đã đọc cho các tin nhắn do đối phương gửi đến mình
                var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<List<Message>>.SuccessResponse(messages, "Tải cuộc trò chuyện thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải tin nhắn");
                return StatusCode(500, ApiResponse<List<Message>>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Message/chats
        [HttpGet("chats")]
        public async Task<ActionResult<ApiResponse<object>>> GetActiveChats()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Không xác định được người dùng"));
                }

                // Lấy tất cả tin nhắn liên quan đến người dùng hiện tại
                var allUserMsgs = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                // Nhóm theo người trò chuyện kia
                var chats = allUserMsgs
                    .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                    .Select(g => {
                        var otherUser = g.First().SenderId == currentUserId ? g.First().Receiver : g.First().Sender;
                        var lastMsg = g.First();
                        var unreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead);

                        return new
                        {
                            otherUserId = otherUser.UserId,
                            fullName = otherUser.FullName,
                            email = otherUser.Email,
                            avatarUrl = otherUser.AvatarUrl ?? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(otherUser.FullName)}&background=2563eb&color=fff",
                            lastMessage = lastMsg.Content,
                            lastMessageTime = lastMsg.CreatedAt,
                            unreadCount = unreadCount
                        };
                    })
                    .ToList();

                return Ok(ApiResponse<object>.SuccessResponse(chats, "Tải danh sách trò chuyện thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách trò chuyện");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Message/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserProfile(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.UserId == userId && u.IsActive)
                    .Select(u => new
                    {
                        userId = u.UserId,
                        fullName = u.FullName,
                        email = u.Email,
                        avatarUrl = u.AvatarUrl ?? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.FullName)}&background=2563eb&color=fff"
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(user, "Lấy thông tin người dùng thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi lấy thông tin người dùng {userId}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Message/admin
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<object>>> GetAdminProfile()
        {
            try
            {
                var admin = await _context.Users
                    .Where(u => u.Role == "Admin" && u.IsActive)
                    .Select(u => new
                    {
                        userId = u.UserId,
                        fullName = u.FullName,
                        email = u.Email,
                        avatarUrl = u.AvatarUrl ?? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.FullName)}&background=2563eb&color=fff"
                    })
                    .FirstOrDefaultAsync();

                if (admin == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy quản trị viên hệ thống"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(admin, "Lấy thông tin quản trị viên thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy thông tin quản trị viên");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Message/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadMessageCount()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<int>.ErrorResponse("Không xác định được người dùng"));
                }

                var count = await _context.Messages
                    .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);

                return Ok(ApiResponse<int>.SuccessResponse(count, "Lấy số tin nhắn chưa đọc thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy số tin nhắn chưa đọc");
                return StatusCode(500, ApiResponse<int>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}
