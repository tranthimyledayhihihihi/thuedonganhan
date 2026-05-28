using WEB.Models;

namespace WEB.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;

        public AuthService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ApiResponse<AuthResponse>?> LoginAsync(LoginViewModel model)
        {
            var loginRequest = new
            {
                Email = model.Email,      // ✅ FIX: PascalCase để khớp với backend
                Password = model.Password // ✅ FIX: PascalCase để khớp với backend
            };

            return await _apiService.PostAsync<object, ApiResponse<AuthResponse>>("Auth/login", loginRequest);
        }

        public async Task<ApiResponse<AuthResponse>?> RegisterAsync(RegisterViewModel model)
        {
            var registerRequest = new
            {
                FullName = model.FullName,         // ✅ FIX: PascalCase
                StudentCode = model.StudentCode,   // ✅ FIX: PascalCase
                Email = model.Email,               // ✅ FIX: PascalCase
                PhoneNumber = model.PhoneNumber,   // ✅ FIX: PascalCase
                Password = model.Password,         // ✅ FIX: PascalCase
                UserType = "Both"                  // ✅ FIX: Thêm UserType
            };

            return await _apiService.PostAsync<object, ApiResponse<AuthResponse>>("Auth/register", registerRequest);
        }

        public async Task<ApiResponse<AuthResponse>?> VerifyOtpAsync(VerifyOtpViewModel model)
        {
            var verifyRequest = new
            {
                Email = model.Email,
                OTPCode = model.OTPCode
            };

            return await _apiService.PostAsync<object, ApiResponse<AuthResponse>>("Auth/verify-otp", verifyRequest);
        }
    }
}
