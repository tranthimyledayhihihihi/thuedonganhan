using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;
using System.Text.Json;

namespace WEB.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<AdminController> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminController(ApiService apiService, ILogger<AdminController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                _logger.LogInformation("=== ADMIN DASHBOARD: Calling API ===");
                var token = HttpContext.Session.GetString("JWTToken");
                _logger.LogInformation($"JWT Token exists: {!string.IsNullOrEmpty(token)}");
                
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>("Admin/dashboard");
                
                _logger.LogInformation($"API Response Success: {response?.Success}");
                _logger.LogInformation($"API Response Data: {response?.Data.ValueKind}");
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    
                    var viewModel = new AdminDashboardViewModel
                    {
                        Stats = new AdminStats
                        {
                            TotalUsers = data.GetProperty("stats").GetProperty("totalUsers").GetInt32(),
                            TotalProducts = data.GetProperty("stats").GetProperty("totalProducts").GetInt32(),
                            TotalRentals = data.GetProperty("stats").GetProperty("totalRentals").GetInt32(),
                            ActiveRentals = data.GetProperty("stats").GetProperty("activeRentals").GetInt32(),
                            PendingRentals = data.GetProperty("stats").GetProperty("pendingRentals").GetInt32(),
                            CompletedRentals = data.GetProperty("stats").GetProperty("completedRentals").GetInt32(),
                            TotalRevenue = data.GetProperty("stats").GetProperty("totalRevenue").GetDecimal(),
                            TotalCommission = data.GetProperty("stats").GetProperty("totalCommission").GetDecimal(),
                            AdminBalance = data.GetProperty("stats").GetProperty("adminBalance").GetDecimal()
                        },
                        RecentCommissions = JsonSerializer.Deserialize<List<AdminCommissionTransaction>>(
                            data.GetProperty("recentCommissions").GetRawText(), _jsonOptions) ?? new()
                    };

                    _logger.LogInformation($"ViewModel created successfully. TotalUsers: {viewModel.Stats.TotalUsers}");
                    return View(viewModel);
                }
                else
                {
                    _logger.LogWarning($"API call failed or returned no data. Success: {response?.Success}, Message: {response?.Message}");
                    TempData["ErrorMessage"] = response?.Message ?? "Không thể tải dữ liệu dashboard";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            _logger.LogWarning("Returning empty AdminDashboardViewModel");
            return View(new AdminDashboardViewModel());
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(int page = 1, string? search = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/users?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(search))
                    endpoint += $"&search={Uri.EscapeDataString(search)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminUserViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminUserViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Search = search;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["ErrorMessage"] = "Không thể tải danh sách người dùng";
            }

            return View(new AdminPagedResult<AdminUserViewModel>());
        }

        // GET: /Admin/Rentals
        public async Task<IActionResult> Rentals(int page = 1, string? status = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/rentals?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(status))
                    endpoint += $"&status={Uri.EscapeDataString(status)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminRentalViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminRentalViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Status = status;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rentals");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn thuê";
            }

            return View(new AdminPagedResult<AdminRentalViewModel>());
        }

        // GET: /Admin/Commissions
        public async Task<IActionResult> Commissions(int page = 1)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>($"Admin/commissions?page={page}&pageSize=20");
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var viewModel = new AdminCommissionsViewModel
                    {
                        Items = JsonSerializer.Deserialize<List<AdminCommissionTransaction>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        AdminBalance = data.GetProperty("adminBalance").GetDecimal(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading commissions");
                TempData["ErrorMessage"] = "Không thể tải danh sách hoa hồng";
            }

            return View(new AdminCommissionsViewModel());
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Products(int page = 1, string? search = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/products?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(search))
                    endpoint += $"&search={Uri.EscapeDataString(search)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminProductViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminProductViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Search = search;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Không thể tải danh sách sản phẩm";
            }

            return View(new AdminPagedResult<AdminProductViewModel>());
        }

        // POST: /Admin/ToggleUserActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(int userId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.PutAsync<object, ApiResponse<bool>>(
                    $"Admin/users/{userId}/toggle-active", new { });

                if (response?.Success == true)
                    TempData["SuccessMessage"] = response.Message;
                else
                    TempData["ErrorMessage"] = response?.Message ?? "Thao tác thất bại";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active");
                TempData["ErrorMessage"] = "Không thể thực hiện thao tác";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.DeleteAsync<ApiResponse<bool>>($"Admin/products/{productId}");

                if (response?.Success == true)
                    TempData["SuccessMessage"] = response.Message;
                else
                    TempData["ErrorMessage"] = response?.Message ?? "Xóa sản phẩm thất bại";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm";
            }

            return RedirectToAction(nameof(Products));
        }

        // GET: /Admin/Students
        public async Task<IActionResult> Students(int page = 1, string? search = null, string? status = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/students?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(search))
                    endpoint += $"&search={Uri.EscapeDataString(search)}";
                if (!string.IsNullOrWhiteSpace(status))
                    endpoint += $"&status={Uri.EscapeDataString(status)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminStudentViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminStudentViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Search = search;
                    ViewBag.Status = status;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");
                TempData["ErrorMessage"] = "Không thể tải danh sách sinh viên";
            }

            return View(new AdminPagedResult<AdminStudentViewModel>());
        }

        // GET: /Admin/SaleOrders
        public async Task<IActionResult> SaleOrders(int page = 1, string? status = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/sale-orders?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(status))
                    endpoint += $"&status={Uri.EscapeDataString(status)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminSaleOrderViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminSaleOrderViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Status = status;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sale orders");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn mua";
            }

            return View(new AdminPagedResult<AdminSaleOrderViewModel>());
        }

        // GET: /Admin/Payments
        public async Task<IActionResult> Payments(int page = 1, string? paymentType = null, string? paymentStatus = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/payments?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(paymentType))
                    endpoint += $"&paymentType={Uri.EscapeDataString(paymentType)}";
                if (!string.IsNullOrWhiteSpace(paymentStatus))
                    endpoint += $"&paymentStatus={Uri.EscapeDataString(paymentStatus)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminPaymentViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminPaymentViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.PaymentType = paymentType;
                    ViewBag.PaymentStatus = paymentStatus;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments");
                TempData["ErrorMessage"] = "Không thể tải danh sách thanh toán";
            }

            return View(new AdminPagedResult<AdminPaymentViewModel>());
        }

        // GET: /Admin/Transactions
        public async Task<IActionResult> Transactions(int page = 1, string? type = null, int? userId = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/transactions?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(type))
                    endpoint += $"&type={Uri.EscapeDataString(type)}";
                if (userId.HasValue)
                    endpoint += $"&userId={userId.Value}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminTransactionViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminTransactionViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.Type = type;
                    ViewBag.UserId = userId;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transactions");
                TempData["ErrorMessage"] = "Không thể tải danh sách giao dịch";
            }

            return View(new AdminPagedResult<AdminTransactionViewModel>());
        }

        // GET: /Admin/Categories
        public async Task<IActionResult> Categories()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.GetAsync<ApiResponse<List<AdminCategoryViewModel>>>("Admin/categories");
                
                if (response?.Success == true && response.Data != null)
                {
                    return View(response.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["ErrorMessage"] = "Không thể tải danh sách danh mục";
            }

            return View(new List<AdminCategoryViewModel>());
        }

        // GET: /Admin/Reviews
        public async Task<IActionResult> Reviews(int page = 1, int? productId = null, int? rating = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/reviews?page={page}&pageSize=20";
                if (productId.HasValue)
                    endpoint += $"&productId={productId.Value}";
                if (rating.HasValue)
                    endpoint += $"&rating={rating.Value}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);
                
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var result = new AdminPagedResult<AdminReviewViewModel>
                    {
                        Items = JsonSerializer.Deserialize<List<AdminReviewViewModel>>(
                            data.GetProperty("items").GetRawText(), _jsonOptions) ?? new(),
                        Pagination = JsonSerializer.Deserialize<AdminPagination>(
                            data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new()
                    };

                    ViewBag.ProductId = productId;
                    ViewBag.Rating = rating;
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews");
                TempData["ErrorMessage"] = "Không thể tải danh sách đánh giá";
            }

            return View(new AdminPagedResult<AdminReviewViewModel>());
        }

        // GET: /Admin/Statistics
        public async Task<IActionResult> Statistics()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.GetAsync<ApiResponse<AdminStatisticsViewModel>>("Admin/statistics");
                
                if (response?.Success == true && response.Data != null)
                {
                    return View(response.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
                TempData["ErrorMessage"] = "Không thể tải thống kê";
            }

            return View(new AdminStatisticsViewModel());
        }

        // POST: /Admin/ApproveProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<bool>>(
                    $"Admin/approve-product/{productId}", new { });

                if (response?.Success == true)
                    TempData["SuccessMessage"] = response.Message;
                else
                    TempData["ErrorMessage"] = response?.Message ?? "Duyệt sản phẩm thất bại";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving product");
                TempData["ErrorMessage"] = "Không thể thực hiện thao tác";
            }

            return RedirectToAction(nameof(Products));
        }

        // POST: /Admin/ApproveAllProducts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllProducts()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<bool>>(
                    "Admin/approve-all-products", new { });

                if (response?.Success == true)
                    TempData["SuccessMessage"] = response.Message;
                else
                    TempData["ErrorMessage"] = response?.Message ?? "Duyệt toàn bộ thất bại";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving all products");
                TempData["ErrorMessage"] = "Không thể thực hiện thao tác";
            }

            return RedirectToAction(nameof(Products));
        }
    }
}
