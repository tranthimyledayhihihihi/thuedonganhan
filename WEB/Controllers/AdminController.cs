using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;
using System.Text.Json;

namespace WEB.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ComplaintService _complaintService;
        private readonly ILogger<AdminController> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminController(ApiService apiService, ComplaintService complaintService, ILogger<AdminController> logger)
        {
            _apiService = apiService;
            _complaintService = complaintService;
            _logger = logger;
        }

        // Helper method để lấy số lượng sản phẩm chờ duyệt
        private async Task SetPendingProductsCountAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>("Admin/pending-products?page=1&pageSize=1");
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var pagination = response.Data.GetProperty("pagination");
                    ViewBag.PendingProductsCount = pagination.GetProperty("totalItems").GetInt32();
                }
                else
                {
                    ViewBag.PendingProductsCount = 0;
                }
            }
            catch
            {
                ViewBag.PendingProductsCount = 0;
            }
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

            await SetPendingProductsCountAsync();

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
        public async Task<IActionResult> Users(int page = 1, string? search = null, string? filterType = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            await SetPendingProductsCountAsync();

            try
            {
                var endpoint = $"Admin/users?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(search))
                    endpoint += $"&search={Uri.EscapeDataString(search)}";

                // Truyền filterType xuống API nếu API hỗ trợ, hoặc lọc client-side
                if (!string.IsNullOrWhiteSpace(filterType) && filterType != "all")
                    endpoint += $"&userType={Uri.EscapeDataString(filterType)}";

                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>(endpoint);

                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var data = response.Data;
                    var allItems = JsonSerializer.Deserialize<List<AdminUserViewModel>>(
                        data.GetProperty("items").GetRawText(), _jsonOptions) ?? new();
                    var pagination = JsonSerializer.Deserialize<AdminPagination>(
                        data.GetProperty("pagination").GetRawText(), _jsonOptions) ?? new();

                    // Client-side filter nếu API chưa hỗ trợ filterType
                    var filtered = filterType switch
                    {
                        "student" => allItems.Where(u => u.UserType == "Student").ToList(),
                        "lender"  => allItems.Where(u => u.UserType == "Lender" || u.UserType == "Owner").ToList(),
                        "locked"  => allItems.Where(u => !u.IsActive).ToList(),
                        _         => allItems
                    };

                    var result = new AdminPagedResult<AdminUserViewModel>
                    {
                        Items      = filtered,
                        Pagination = pagination
                    };

                    ViewBag.Search     = search;
                    ViewBag.FilterType = filterType ?? "all";

                    // Stats cho stat cards (tính từ toàn bộ danh sách trang hiện tại)
                    ViewBag.TotalStudents = allItems.Count(u => u.UserType == "Student");
                    ViewBag.TotalLenders  = allItems.Count(u => u.UserType == "Lender" || u.UserType == "Owner");
                    ViewBag.TotalLocked   = allItems.Count(u => !u.IsActive);

                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["ErrorMessage"] = "Không thể tải danh sách người dùng";
            }

            ViewBag.Search     = search;
            ViewBag.FilterType = filterType ?? "all";
            return View(new AdminPagedResult<AdminUserViewModel>());
        }

        // GET: /Admin/Rentals
        public async Task<IActionResult> Rentals(int page = 1, string? status = null,
            string? fromDate = null, string? toDate = null)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var endpoint = $"Admin/rentals?page={page}&pageSize=20";
                if (!string.IsNullOrWhiteSpace(status))
                    endpoint += $"&status={Uri.EscapeDataString(status)}";
                if (!string.IsNullOrWhiteSpace(fromDate))
                    endpoint += $"&fromDate={Uri.EscapeDataString(fromDate)}";
                if (!string.IsNullOrWhiteSpace(toDate))
                    endpoint += $"&toDate={Uri.EscapeDataString(toDate)}";

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

                    ViewBag.Status   = status ?? "";
                    ViewBag.FromDate = fromDate ?? "";
                    ViewBag.ToDate   = toDate ?? "";
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rentals");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn thuê";
            }

            ViewBag.Status   = status ?? "";
            ViewBag.FromDate = fromDate ?? "";
            ViewBag.ToDate   = toDate ?? "";
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
        public async Task<IActionResult> Products(int page = 1, string? search = null, string? filterCat = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            await SetPendingProductsCountAsync();

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

                    ViewBag.Search    = search;
                    ViewBag.FilterCat = filterCat ?? "all";
                    return View(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Không thể tải danh sách sản phẩm";
            }

            ViewBag.Search    = search;
            ViewBag.FilterCat = filterCat ?? "all";
            return View(new AdminPagedResult<AdminProductViewModel>());
        }

        // POST: /Admin/ToggleUserActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(int userId, string? search = null, string? filterType = "all")
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

            return RedirectToAction(nameof(Users), new { search, filterType });
        }

        // POST: /Admin/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productId, string? search = null, string? filterCat = "all")
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

            return RedirectToAction(nameof(Products), new { search, filterCat });
        }

        // POST: /Admin/DeleteBulkProducts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBulkProducts(string productIds, string? search = null, string? filterCat = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(productIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm";
                return RedirectToAction(nameof(Products), new { search, filterCat });
            }

            var ids = productIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            int successCount = 0;
            int failCount = 0;

            foreach (var id in ids)
            {
                try
                {
                    var response = await _apiService.DeleteAsync<ApiResponse<bool>>($"Admin/products/{id}");
                    if (response?.Success == true) successCount++;
                    else failCount++;
                }
                catch
                {
                    failCount++;
                }
            }

            if (successCount > 0)
                TempData["SuccessMessage"] = $"Đã xóa thành công {successCount} sản phẩm" +
                    (failCount > 0 ? $", {failCount} sản phẩm thất bại" : "");
            else
                TempData["ErrorMessage"] = "Không thể xóa các sản phẩm đã chọn";

            return RedirectToAction(nameof(Products), new { search, filterCat });
        }

        // GET: /Admin/EditProduct
        public async Task<IActionResult> EditProduct(int productId, string? search = null, string? filterCat = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var response = await _apiService.GetAsync<ApiResponse<JsonElement>>($"Admin/products/{productId}");
                if (response?.Success == true && response.Data.ValueKind != JsonValueKind.Undefined)
                {
                    var product = JsonSerializer.Deserialize<AdminProductViewModel>(
                        response.Data.GetRawText(), _jsonOptions);
                    ViewBag.Search    = search;
                    ViewBag.FilterCat = filterCat;
                    return View(product);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit");
            }

            TempData["ErrorMessage"] = "Không thể tải thông tin sản phẩm";
            return RedirectToAction(nameof(Products), new { search, filterCat });
        }

        // POST: /Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int productId, string productName,
            decimal pricePerDay, decimal deposit, int quantity,
            string? location, string? description,
            string? search = null, string? filterCat = "all")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var payload = new
                {
                    productName,
                    pricePerDay,
                    deposit,
                    quantity,
                    location,
                    description
                };

                var response = await _apiService.PutAsync<object, ApiResponse<bool>>(
                    $"Admin/products/{productId}", payload);

                if (response?.Success == true)
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công";
                else
                    TempData["ErrorMessage"] = response?.Message ?? "Cập nhật thất bại";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["ErrorMessage"] = "Không thể cập nhật sản phẩm";
            }

            return RedirectToAction(nameof(Products), new { search, filterCat });
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
        public async Task<IActionResult> ApproveProduct(int productId, string? search = null, string? filterCat = "all")
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

            return RedirectToAction(nameof(Products), new { search, filterCat });
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

        // GET: /Admin/Complaints
        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var token = HttpContext.Session.GetString("JWTToken");
            var response = await _complaintService.GetAllComplaintsAsync();

            if (response != null && response.Success)
            {
                return View(response.Data);
            }

            return View(new List<ComplaintViewModel>());
        }

        // POST: /Admin/UpdateComplaintStatus
        [HttpPost]
        public async Task<IActionResult> UpdateComplaintStatus(int complaintId, string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var response = await _complaintService.UpdateComplaintStatusAsync(complaintId, status);

            if (response != null && response.Success)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = response?.Message ?? "Update failed" });
        }
        // POST: /Admin/ResolveComplaint
        [HttpPost]
        public async Task<IActionResult> ResolveComplaint(int complaintId, decimal ownerCompensation, decimal renterRefund, string adminNotes)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var token = HttpContext.Session.GetString("JWTToken");
            var requestData = new
            {
                OwnerCompensation = ownerCompensation,
                RenterRefund = renterRefund,
                AdminNotes = adminNotes
            };

            var response = await _complaintService.ResolveComplaintAsync(complaintId, requestData, token ?? "");

            if (response != null && response.Success)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = response?.Message ?? "Resolution failed" });
        }
    }
}
