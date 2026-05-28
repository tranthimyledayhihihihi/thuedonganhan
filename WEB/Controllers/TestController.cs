using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace WEB.Controllers
{
    public class TestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                
                ViewBag.BaseUrl = client.BaseAddress?.ToString();
                ViewBag.FullUrl = $"{client.BaseAddress}Product";
                
                var response = await client.GetAsync("Product");
                ViewBag.StatusCode = response.StatusCode;
                
                var content = await response.Content.ReadAsStringAsync();
                ViewBag.RawResponse = content;
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<dynamic>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    ViewBag.Success = true;
                }
                else
                {
                    ViewBag.Success = false;
                    ViewBag.Error = $"Status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Success = false;
                ViewBag.Error = ex.Message;
                ViewBag.StackTrace = ex.StackTrace;
            }
            
            return View();
        }
    }
}
