using Microsoft.AspNetCore.Mvc;
using WEB.Services;
using WEB.Filters;

namespace WEB.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: Product - Xem danh sách không cần đăng nhập
        // GET: Product - Xem danh sách không cần đăng nhập
        public async Task<IActionResult> Index(string? keyword, string? categoryIds, decimal? minPrice, decimal? maxPrice, string? sortBy)
        {
            try
            {
                _logger.LogInformation("=== ProductController.Index START ===");
                
                // Build query string for API
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(keyword)) queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
                if (!string.IsNullOrEmpty(categoryIds)) queryParams.Add($"categoryIds={categoryIds}");
                if (minPrice.HasValue) queryParams.Add($"minPrice={minPrice}");
                if (maxPrice.HasValue) queryParams.Add($"maxPrice={maxPrice}");
                if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");
                
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    queryParams.Add($"currentUserId={userId.Value}");
                }
                
                // Pass keyword and filters to ViewBag so the search input can keep it
                ViewBag.Keyword = keyword;
                ViewBag.CategoryIds = categoryIds;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.SortBy = sortBy;
                
                var endpoint = queryParams.Count > 0 
                    ? $"Product/filter?{string.Join("&", queryParams)}"
                    : "Product";
                
                // Fetch categories for sidebar
                var categoriesResponse = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categoriesResponse?.Data ?? new List<WEB.Models.CategoryViewModel>();
                
                var response = await _productService.GetFilteredProductsAsync(endpoint);
                
                _logger.LogInformation($"Response is null: {response == null}");
                if (response != null)
                {
                    _logger.LogInformation($"Response.Success: {response.Success}");
                    _logger.LogInformation($"Response.Message: {response.Message}");
                    _logger.LogInformation($"Response.Data is null: {response.Data == null}");
                    if (response.Data != null)
                    {
                        _logger.LogInformation($"Response.Data.Count: {response.Data.Count}");
                    }
                }
                
                if (response != null && response.Success)
                {
                    _logger.LogInformation($"✅ Returning view with {response.Data?.Count ?? 0} products");
                    return View(response.Data);
                }

                ViewBag.ErrorMessage = response?.Message ?? "Không thể tải danh sách sản phẩm";
                _logger.LogWarning($"⚠️ Returning empty view. ErrorMessage: {ViewBag.ErrorMessage}");
                return View(new List<WEB.Models.ProductViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading products");
                ViewBag.ErrorMessage = "Lỗi kết nối đến server";
                return View(new List<WEB.Models.ProductViewModel>());
            }
        }

        // GET: Product/Details/5 - Xem chi tiết không cần đăng nhập
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _productService.GetProductByIdAsync(id);
                
                if (response != null && response.Success && response.Data != null)
                {
                    // Kiểm tra đã đăng nhập chưa để hiển thị nút thuê
                    var userId = HttpContext.Session.GetInt32("UserId");
                    ViewBag.IsLoggedIn = userId.HasValue;
                    ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
                    
                    // Nếu là chủ sở hữu, không cho phép thuê
                    ViewBag.IsOwner = userId.HasValue && response.Data.OwnerId == userId.Value;
                    
                    return View(response.Data);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details");
                return NotFound();
            }
        }

        // POST: Product/Rent - Thuê đồ YÊU CẦU đăng nhập và là sinh viên UTE
        [HttpPost]
        [UteStudentAuthorization]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int productId, DateTime startDate, int duration, string rentalUnit = "Day")
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để thuê sản phẩm.";
                    return RedirectToAction("Login", "Account");
                }

                if (duration <= 0)
                {
                    TempData["ErrorMessage"] = "Thời gian thuê phải lớn hơn 0.";
                    return RedirectToAction("Details", new { id = productId });
                }

                // Lấy thông tin sản phẩm để kiểm tra
                var productResponse = await _productService.GetProductByIdAsync(productId);
                if (productResponse?.Data == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index");
                }

                var p = productResponse.Data;
                if (p.OwnerId == userId.Value)
                {
                    TempData["ErrorMessage"] = "Bạn không thể thuê sản phẩm do chính mình đăng.";
                    return RedirectToAction("Details", new { id = productId });
                }
                
                // Tính toán thời gian kết thúc và tổng giá trị dựa trên gói thuê và duration
                DateTime endDate;
                decimal pricePerUnit;
                
                if (rentalUnit == "Hour")
                {
                    endDate = startDate.AddHours(duration);
                    pricePerUnit = p.PricePerHour ?? (p.PricePerDay / 24);
                }
                else if (rentalUnit == "Week")
                {
                    endDate = startDate.AddDays(duration * 7);
                    pricePerUnit = p.PricePerWeek ?? (p.PricePerDay * 7);
                }
                else if (rentalUnit == "Month")
                {
                    endDate = startDate.AddMonths(duration);
                    pricePerUnit = p.PricePerMonth ?? (p.PricePerDay * 30);
                }
                else
                {
                    rentalUnit = "Day";
                    endDate = startDate.AddDays(duration);
                    pricePerUnit = p.PricePerDay;
                }

                decimal totalPrice = pricePerUnit * duration;

                var rentalRequest = new WEB.Models.CreateRentalRequest
                {
                    ProductId = productId,
                    Quantity = 1,
                    StartDate = startDate,
                    EndDate = endDate,
                    RentalUnit = rentalUnit,
                    DepositAmount = p.Deposit,
                    TotalPrice = Math.Round(totalPrice, 2)
                };

                var createResponse = await _productService.CreateRentalAsync(rentalRequest);

                if (createResponse != null && createResponse.Success)
                {
                    TempData["SuccessMessage"] = "Đặt thuê thành công! Vui lòng liên hệ chủ sản phẩm để nhận đồ.";
                }
                else
                {
                    TempData["ErrorMessage"] = createResponse?.Message ?? "Có lỗi xảy ra khi đặt thuê. Vui lòng thử lại.";
                }

                return RedirectToAction("Details", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renting product");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi kết nối máy chủ. Vui lòng thử lại.";
                return RedirectToAction("Details", new { id = productId });
            }
        }

        // GET: Product/Search
        public async Task<IActionResult> Search(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return RedirectToAction(nameof(Index));
                }

                var response = await _productService.SearchProductsAsync(keyword);
                
                if (response != null && response.Success)
                {
                    ViewBag.Keyword = keyword;
                    return View("Index", response.Data);
                }

                ViewBag.ErrorMessage = "Không tìm thấy sản phẩm nào";
                return View("Index", new List<WEB.Models.ProductViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                ViewBag.ErrorMessage = "Lỗi kết nối đến server";
                return View("Index", new List<WEB.Models.ProductViewModel>());
            }
        }

        // GET: Product/GetBookedDates/5
        [HttpGet]
        public async Task<IActionResult> GetBookedDates(int id)
        {
            try
            {
                var response = await _productService.GetBookedDatesAsync(id);
                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching booked dates for product {id}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Product/Cart
        [HttpGet]
        public IActionResult Cart()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public class CartItemCheckoutModel
        {
            public int ProductId { get; set; }
            public string RentalUnit { get; set; } = "Day";
            public int Duration { get; set; }
            public string StartDate { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutCart([FromBody] List<CartItemCheckoutModel> items)
        {
            if (items == null || items.Count == 0)
            {
                return Json(new { success = false, message = "Giỏ hàng rỗng." });
            }

            try
            {
                int successCount = 0;
                string lastError = "";

                foreach (var item in items)
                {
                    // Fetch product details
                    var pRes = await _productService.GetProductByIdAsync(item.ProductId);
                    if (pRes == null || !pRes.Success || pRes.Data == null)
                    {
                        lastError = $"Không tìm thấy sản phẩm {item.ProductId}";
                        continue;
                    }

                    var p = pRes.Data;

                    if (item.Duration <= 0)
                    {
                        lastError = $"Thời gian thuê sản phẩm {item.ProductId} phải lớn hơn 0.";
                        continue;
                    }

                    // Parse start date — skip item nếu ngày không hợp lệ
                    if (!DateTime.TryParse(item.StartDate, out DateTime start))
                    {
                        lastError = $"Ngày bắt đầu không hợp lệ cho sản phẩm {item.ProductId}.";
                        continue;
                    }
                    DateTime end = start;

                    if (item.RentalUnit == "Hour") end = start.AddHours(item.Duration);
                    else if (item.RentalUnit == "Week") end = start.AddDays(item.Duration * 7);
                    else if (item.RentalUnit == "Month") end = start.AddMonths(item.Duration);
                    else end = start.AddDays(item.Duration); // Day

                    decimal pricePerUnit = p.PricePerDay;
                    if (item.RentalUnit == "Hour") pricePerUnit = p.PricePerHour ?? 0;
                    else if (item.RentalUnit == "Week") pricePerUnit = p.PricePerWeek ?? 0;
                    else if (item.RentalUnit == "Month") pricePerUnit = p.PricePerMonth ?? 0;

                    if (pricePerUnit <= 0)
                    {
                        lastError = $"Sản phẩm {item.ProductId} không có giá hợp lệ cho gói thuê này.";
                        continue;
                    }

                    decimal totalPrice = pricePerUnit * item.Duration;

                    var rentalRequest = new WEB.Models.CreateRentalRequest
                    {
                        ProductId = item.ProductId,
                        Quantity = 1,
                        StartDate = start,
                        EndDate = end,
                        RentalUnit = item.RentalUnit,
                        DepositAmount = p.Deposit,
                        TotalPrice = Math.Round(totalPrice, 2)
                    };

                    var createResponse = await _productService.CreateRentalAsync(rentalRequest);
                    if (createResponse != null && createResponse.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        lastError = createResponse?.Message ?? "Lỗi đặt thuê.";
                    }
                }

                if (successCount == items.Count)
                {
                    return Json(new { success = true, message = "Đặt thuê tất cả sản phẩm thành công!" });
                }
                else if (successCount > 0)
                {
                    return Json(new { success = true, message = $"Đặt thuê thành công {successCount}/{items.Count} sản phẩm. Lỗi: {lastError}" });
                }
                else
                {
                    return Json(new { success = false, message = $"Thất bại: {lastError}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out cart");
                return Json(new { success = false, message = "Lỗi hệ thống khi thanh toán giỏ hàng." });
            }
        }
    }
}
