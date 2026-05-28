using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;
using System.Diagnostics;

namespace WEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProductService _productService;
        private readonly ApiService _apiService;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, 
            ProductService productService, ApiService apiService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _productService = productService;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index(string mode = null)
        {
            // Kiểm tra đã đăng nhập chưa
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            var userId = HttpContext.Session.GetInt32("UserId");
            
            ViewBag.IsLoggedIn = isLoggedIn;
            
            // Mặc định luôn hiển thị trang chủ (home) nếu không có mode
            ViewBag.Mode = string.IsNullOrEmpty(mode) ? "home" : mode;
            
            try
            {
                if (ViewBag.Mode == "owner" && isLoggedIn)
                {
                    // Load sản phẩm của user cho tab "Người Cho Thuê"
                    if (userId.HasValue)
                    {
                        var myProductsResponse = await _productService.GetProductsByUserIdAsync(userId.Value);
                        ViewBag.MyProducts = myProductsResponse?.Data ?? new List<ProductViewModel>();
                    }
                    else
                    {
                        ViewBag.MyProducts = new List<ProductViewModel>();
                    }
                    ViewBag.AllProducts = new List<ProductViewModel>();
                }
                else if (ViewBag.Mode == "renter" && isLoggedIn)
                {
                    // mode = "renter": Load tất cả sản phẩm cho người thuê (không gồm sản phẩm của chính mình)
                    var endpoint = userId.HasValue ? $"Product?currentUserId={userId.Value}" : "Product";
                    var allProductsResponse = await _productService.GetFilteredProductsAsync(endpoint);
                    ViewBag.AllProducts = allProductsResponse?.Data ?? new List<ProductViewModel>();
                    ViewBag.MyProducts = new List<ProductViewModel>();
                }
                else
                {
                    // mode = "home": Trang chủ bình thường
                    // Load top 6 sản phẩm mới nhất
                    var endpoint = userId.HasValue ? $"Product/filter?pageSize=6&currentUserId={userId.Value}" : "Product/filter?pageSize=6";
                    var homeProductsResponse = await _productService.GetFilteredProductsAsync(endpoint);
                    ViewBag.AllProducts = homeProductsResponse?.Data ?? new List<ProductViewModel>();
                    ViewBag.MyProducts = new List<ProductViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for home page");
                ViewBag.AllProducts = new List<ProductViewModel>();
                ViewBag.MyProducts = new List<ProductViewModel>();
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> RenterDashboard()
        {
            // Kiểm tra đã đăng nhập chưa
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            
            if (!isLoggedIn)
            {
                return RedirectToAction("Index");
            }
            
            try
            {
                // Load tất cả sản phẩm cho người thuê (trừ sản phẩm của chính mình)
                var userId = HttpContext.Session.GetInt32("UserId");
                var endpoint = userId.HasValue ? $"Product?currentUserId={userId.Value}" : "Product";
                var allProductsResponse = await _productService.GetFilteredProductsAsync(endpoint);
                ViewBag.AllProducts = allProductsResponse?.Data ?? new List<ProductViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for renter dashboard");
                ViewBag.AllProducts = new List<ProductViewModel>();
            }
            
            return View();
        }

        public async Task<IActionResult> OwnerDashboard()
        {
            // Kiểm tra đã đăng nhập chưa
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            var userId = HttpContext.Session.GetInt32("UserId");
            
            if (!isLoggedIn || !userId.HasValue)
            {
                return RedirectToAction("Index");
            }
            
            try
            {
                // Load sản phẩm của user cho người cho thuê
                var myProductsResponse = await _productService.GetProductsByUserIdAsync(userId.Value);
                ViewBag.MyProducts = myProductsResponse?.Data ?? new List<ProductViewModel>();
                
                // Load danh sách đơn thuê của các sản phẩm này
                var myRentalsResponse = await _productService.GetRentalsByOwnerAsync(userId.Value);
                ViewBag.MyRentals = myRentalsResponse?.Data ?? new List<RentalViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for owner dashboard");
                ViewBag.MyProducts = new List<ProductViewModel>();
                ViewBag.MyRentals = new List<RentalViewModel>();
            }
            
            return View();
        }

        public async Task<IActionResult> OwnerRentalDetails(int id)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            var userId = HttpContext.Session.GetInt32("UserId");
            
            if (!isLoggedIn || !userId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var rentalResponse = await _productService.GetRentalByIdAsync(id);
            if (rentalResponse?.Data == null)
            {
                return NotFound();
            }

            return View(rentalResponse.Data);
        }

        // ✅ POST: /Home/ConfirmDelivery — Người cho thuê xác nhận giao hàng
        // Dòng tiền: 
        // - Tiền thuê (95%) → ví owner
        // - 5% hoa hồng → ví admin
        // - Tiền cọc giữ nguyên (chờ trả đồ mới hoàn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(int rentalId)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            if (!isLoggedIn)
                return RedirectToAction("Login", "Account");

            var response = await _apiService.PutAsync<object, ApiResponse<RentalViewModel>>(
                $"Rental/{rentalId}/deliver", new { });

            if (response?.Success == true)
                TempData["SuccessMessage"] = response.Message ?? "Xác nhận giao hàng thành công! Bạn đã nhận 95% giá trị đơn thuê vào ví.";
            else
                TempData["ErrorMessage"] = response?.Message ?? "Xác nhận giao hàng thất bại!";

            return RedirectToAction(nameof(OwnerDashboard));
        }

        // ✅ POST: /Home/ConfirmReturn — Người cho thuê xác nhận đã nhận lại đồ
        // → Hoàn tiền cọc cho người thuê
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(int rentalId)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            if (!isLoggedIn)
                return RedirectToAction("Login", "Account");

            var response = await _apiService.PutAsync<object, ApiResponse<RentalViewModel>>(
                $"Rental/{rentalId}/complete", new { });

            if (response?.Success == true)
                TempData["SuccessMessage"] = response.Message ?? "Hoàn thành đơn thuê! Đã hoàn tiền cọc cho người thuê.";
            else
                TempData["ErrorMessage"] = response?.Message ?? "Thao tác thất bại!";

            return RedirectToAction(nameof(OwnerDashboard));
        }

        // ✅ POST: /Home/ConfirmRental — Người cho thuê xác nhận đơn (Pending → Confirmed)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRental(int rentalId)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            if (!isLoggedIn)
                return RedirectToAction("Login", "Account");

            var response = await _apiService.PutAsync<object, ApiResponse<RentalViewModel>>(
                $"Rental/{rentalId}/confirm", new { });

            if (response?.Success == true)
                TempData["SuccessMessage"] = response.Message ?? "Đã xác nhận đơn thuê!";
            else
                TempData["ErrorMessage"] = response?.Message ?? "Xác nhận thất bại!";

            return RedirectToAction(nameof(OwnerDashboard));
        }

        // ✅ POST: /Home/CancelRental — Hủy đơn thuê (hoàn toàn bộ tiền)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRental(int rentalId)
        {
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            if (!isLoggedIn)
                return RedirectToAction("Login", "Account");

            var response = await _apiService.PutAsync<object, ApiResponse<RentalViewModel>>(
                $"Rental/{rentalId}/cancel", new { });

            if (response?.Success == true)
                TempData["SuccessMessage"] = response.Message ?? "Đã hủy đơn thuê!";
            else
                TempData["ErrorMessage"] = response?.Message ?? "Hủy đơn thất bại!";

            return RedirectToAction(nameof(OwnerDashboard));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
