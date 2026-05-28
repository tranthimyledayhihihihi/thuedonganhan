using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WEB.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            
            // Lấy token từ Session
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var client = CreateClient();
                Console.WriteLine($"[ApiService] Calling: {client.BaseAddress}{endpoint}");
                
                var response = await client.GetAsync(endpoint);
                Console.WriteLine($"[ApiService] Status: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[ApiService] Unauthorized (401) returned from backend. Clearing session.");
                    _httpContextAccessor.HttpContext?.Session.Clear();
                    
                    if (typeof(T).Name.Contains("ApiResponse"))
                    {
                        dynamic errorResponse = Activator.CreateInstance(typeof(T))!;
                        errorResponse.Success = false;
                        errorResponse.Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!";
                        return errorResponse;
                    }
                    return default;
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Response: {content.Substring(0, Math.Min(200, content.Length))}...");
                    
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Error Response: {errorContent}");
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Exception in GetAsync: {ex.Message}");
                Console.WriteLine($"[ApiService] StackTrace: {ex.StackTrace}");
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var client = CreateClient();
                
                // Log request
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"[ApiService] POST {client.BaseAddress}{endpoint}");
                Console.WriteLine($"[ApiService] Request Body: {json}");
                
                // Check token
                var token = _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
                Console.WriteLine($"[ApiService] JWT Token: {(string.IsNullOrEmpty(token) ? "MISSING" : "Present")}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                
                Console.WriteLine($"[ApiService] Response Status: {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[ApiService] Unauthorized (401) returned from backend. Clearing session.");
                    _httpContextAccessor.HttpContext?.Session.Clear();
                    
                    if (typeof(TResponse).Name.Contains("ApiResponse"))
                    {
                        dynamic errorResponse = Activator.CreateInstance(typeof(TResponse))!;
                        errorResponse.Success = false;
                        errorResponse.Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!";
                        return errorResponse;
                    }
                    return default;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ApiService] Response Body: {responseContent}");

                try
                {
                    var result = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[ApiService] POST Failed: {response.StatusCode}. Deserialized error.");
                        if (result != null && typeof(TResponse).Name.Contains("ApiResponse"))
                        {
                            // It successfully deserialized the error response from the backend.
                            return result; 
                        }
                    }
                    
                    return result;
                }
                catch
                {
                    Console.WriteLine($"[ApiService] POST Failed to deserialize: {responseContent}");
                    if (typeof(TResponse).Name.Contains("ApiResponse")) {
                        dynamic errorResponse = Activator.CreateInstance(typeof(TResponse))!;
                        errorResponse.Success = false;
                        errorResponse.Message = "Backend Error: " + (responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent);
                        return errorResponse;
                    }
                    return default;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Exception in PostAsync: {ex.Message}");
                Console.WriteLine($"[ApiService] StackTrace: {ex.StackTrace}");
                return default;
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var client = CreateClient();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(endpoint, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[ApiService] Unauthorized (401) returned from backend. Clearing session.");
                    _httpContextAccessor.HttpContext?.Session.Clear();
                    
                    if (typeof(TResponse).Name.Contains("ApiResponse"))
                    {
                        dynamic errorResponse = Activator.CreateInstance(typeof(TResponse))!;
                        errorResponse.Success = false;
                        errorResponse.Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!";
                        return errorResponse;
                    }
                    return default;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                try {
                    return System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                } catch {
                    Console.WriteLine($"[ApiService] PUT Failed to deserialize: {responseContent}");
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PutAsync: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var client = CreateClient();
                var response = await client.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<T?> DeleteAsync<T>(string endpoint)
        {
            try
            {
                var client = CreateClient();
                var response = await client.DeleteAsync(endpoint);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[ApiService] Unauthorized (401) returned from backend. Clearing session.");
                    _httpContextAccessor.HttpContext?.Session.Clear();
                    
                    if (typeof(T).Name.Contains("ApiResponse"))
                    {
                        dynamic errorResponse = Activator.CreateInstance(typeof(T))!;
                        errorResponse.Success = false;
                        errorResponse.Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!";
                        return errorResponse;
                    }
                    return default;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                try
                {
                    return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                    Console.WriteLine($"[ApiService] DELETE Failed to deserialize: {responseContent}");
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync<T>: {ex.Message}");
                return default;
            }
        }
    }
}
