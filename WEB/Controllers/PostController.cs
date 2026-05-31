using Microsoft.AspNetCore.Mvc;
using WEB.Filters;
using WEB.Services;
using WEB.Models;

namespace WEB.Controllers
{
    [UteStudentAuthorization] // Yêu cầu đăng nhập và là sinh viên UTE
    public class PostController : Controller
    {
        private readonly ProductService _productService;
        private readonly ILogger<PostController> _logger;

        public PostController(ProductService productService, ILogger<PostController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                // Lấy thông tin user từ session để hiển thị
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
                
                // Lấy danh sách categories từ API
                var categoriesResponse = await _productService.GetAllCategoriesAsync();
                if (categoriesResponse != null && categoriesResponse.Success)
                {
                    ViewBag.Categories = categoriesResponse.Data;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryViewModel>();
                    _logger.LogWarning("Không thể tải danh sách danh mục");
                }
                
                // Truyền model rỗng cho view
                return View(new CreateProductRequest());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product page");
                ViewBag.Categories = new List<CategoryViewModel>();
                return View(new CreateProductRequest());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductRequest request)
        {
            try
            {
                // Kiểm tra lại session (double check)
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userId = HttpContext.Session.GetInt32("UserId");
                var jwtToken = HttpContext.Session.GetString("JWTToken");
                
                if (string.IsNullOrEmpty(userEmail) || !userId.HasValue || string.IsNullOrEmpty(jwtToken))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để đăng tin!";
                    return RedirectToAction("Login", "Account");
                }
                
                // Gán OwnerId từ session
                request.OwnerId = userId.Value;

                // Giải mã JSON danh sách hình ảnh
                if (!string.IsNullOrEmpty(request.ProductImagesJson))
                {
                    try
                    {
                        var imagesList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(request.ProductImagesJson);
                        if (imagesList != null && imagesList.Count > 0)
                        {
                            request.ProductImages = imagesList;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi giải mã ProductImagesJson");
                    }
                }
                
                // Gọi API để tạo product
                var response = await _productService.CreateProductAsync(request, jwtToken);
                
                if (response != null && response.Success)
                {
                    TempData["SuccessMessage"] = "Đăng tin thành công!";
                    return RedirectToAction("Index", "Home", new { mode = "owner" });
                }
                else
                {
                    TempData["ErrorMessage"] = response?.Message ?? "Có lỗi xảy ra khi đăng tin";
                    
                    // Load lại categories để hiển thị form
                    var categoriesResponse = await _productService.GetAllCategoriesAsync();
                    ViewBag.Categories = categoriesResponse?.Data ?? new List<CategoryViewModel>();
                    ViewBag.UserName = HttpContext.Session.GetString("UserName");
                    ViewBag.UserEmail = userEmail;
                    
                    return View(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng tin. Vui lòng thử lại.";
                
                // Load lại categories để hiển thị form
                var categoriesResponse = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categoriesResponse?.Data ?? new List<CategoryViewModel>();
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
                
                return View(request);
            }
        }
    }
}
