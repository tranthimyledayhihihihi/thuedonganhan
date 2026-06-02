using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WEB.Filters;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers
{
    [UteStudentAuthorization] // Yêu cầu đăng nhập và là sinh viên UTE
    public class MessageController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(ApiService apiService, ILogger<MessageController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // GET: /Message
        // GET: /Message?receiverId=X&productId=Y
        public IActionResult Index(int? receiverId, int? productId)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("UserId");
            
            if (receiverId.HasValue)
            {
                ViewBag.ReceiverId = receiverId.Value;
                _logger.LogInformation($"Khởi động chat với ReceiverId: {receiverId}");
            }

            if (productId.HasValue)
            {
                ViewBag.ProductId = productId.Value;
            }

            return View();
        }

        // GET: /Message/GetChats
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>("Message/chats");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể tải danh sách trò chuyện" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API chats");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // GET: /Message/GetConversation?otherUserId=X
        [HttpGet]
        public async Task<IActionResult> GetConversation(int otherUserId)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>($"Message/conversation/{otherUserId}");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể tải tin nhắn" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải cuộc trò chuyện với {otherUserId}");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // POST: /Message/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string content, int? productId = null, int? rentalId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Tin nhắn không được để trống" });
            }

            try
            {
                var payload = new
                {
                    receiverId = receiverId,
                    content = content,
                    productId = productId,
                    rentalId = rentalId
                };

                var response = await _apiService.PostAsync<object, ApiResponse<JsonElement>>("Message", payload);
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Gửi tin nhắn thất bại" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi tin nhắn");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // GET: /Message/GetUserProfile?userId=X
        [HttpGet]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>($"Message/user/{userId}");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể lấy thông tin người dùng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin người dùng {userId}");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // GET: /Message/GetProductDetails?productId=X
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>($"Product/{productId}");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể lấy thông tin sản phẩm" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin sản phẩm {productId}");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }
    }
}
