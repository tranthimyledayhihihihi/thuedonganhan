using Microsoft.AspNetCore.Mvc;
using WEB.Services;

namespace WEB.Controllers
{
    public class TestProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly ILogger<TestProductController> _logger;

        public TestProductController(ProductService productService, ILogger<TestProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: /TestProduct - Debug endpoint
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("=== TestProductController.Index START ===");
            
            try
            {
                var response = await _productService.GetAllProductsAsync();
                
                _logger.LogInformation($"Response received: {response != null}");
                
                var debugInfo = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ResponseIsNull = response == null,
                    Success = response?.Success,
                    Message = response?.Message,
                    DataIsNull = response?.Data == null,
                    DataCount = response?.Data?.Count ?? 0,
                    FirstProduct = response?.Data?.FirstOrDefault(),
                    AllProducts = response?.Data
                };
                
                _logger.LogInformation($"Debug Info: {System.Text.Json.JsonSerializer.Serialize(debugInfo)}");
                
                return Json(debugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestProductController");
                
                return Json(new
                {
                    Error = true,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        // GET: /TestProduct/Raw - Test raw API call
        public async Task<IActionResult> Raw()
        {
            _logger.LogInformation("=== TestProductController.Raw START ===");
            
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:7000/api/");
                
                var response = await httpClient.GetAsync("Product");
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Raw API Response: {content}");
                
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestProductController.Raw");
                
                return Json(new
                {
                    Error = true,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
