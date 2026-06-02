using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WEB.Filters;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers
{
    [Route("[controller]/[action]")]
    public class NotificationController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ApiService apiService, ILogger<NotificationController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // GET: /Notification/GetNotifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>("Notification");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể lấy danh sách thông báo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API danh sách thông báo");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // GET: /Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<int>>("Notification/unread-count");
                if (response != null && response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể lấy số lượng thông báo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API số lượng thông báo");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // POST: /Notification/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var response = await _apiService.PutAsync<object, ApiResponse<bool>>($"Notification/mark-read/{id}", new { });
                if (response != null && response.Success)
                {
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể cập nhật trạng thái thông báo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đánh dấu thông báo {id} đã đọc");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }

        // POST: /Notification/MarkAllRead
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var response = await _apiService.PutAsync<object, ApiResponse<bool>>("Notification/mark-all-read", new { });
                if (response != null && response.Success)
                {
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = response?.Message ?? "Không thể cập nhật trạng thái thông báo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu tất cả thông báo đã đọc");
                return Json(new { success = false, message = "Lỗi kết nối máy chủ" });
            }
        }
    }
}
