using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            try
            {
                var response = await _authService.LoginAsync(model);

                if (response != null && response.Success && response.Data != null)
                {
                    HttpContext.Session.SetString("JWTToken", response.Data.Token);
                    HttpContext.Session.SetString("UserName", response.Data.FullName);
                    HttpContext.Session.SetString("UserEmail", response.Data.Email);
                    HttpContext.Session.SetString("UserRole", response.Data.Role);
                    HttpContext.Session.SetInt32("UserId", response.Data.UserId);

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";

                    // ✅ Nếu là Admin → chuyển đến Admin Dashboard
                    if (response.Data.Role == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    // Nếu có returnUrl → redirect về đó
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // User thường → trang chủ
                    return Redirect("/");
                }

                ModelState.AddModelError("", response?.Message ?? "Email hoặc mật khẩu không đúng");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError("", "Lỗi kết nối đến server. Vui lòng thử lại sau.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // [v4.0] Không thêm số 0 vào đầu MSSV nữa
                if (!string.IsNullOrWhiteSpace(model.StudentCode))
                {
                    model.StudentCode = model.StudentCode.Trim();
                }

                var response = await _authService.RegisterAsync(model);

                if (response != null && response.Success && response.Data != null)
                {
                    // Lấy email để truyền sang trang Verify OTP
                    TempData["VerifyEmail"] = response.Data.Email;
                    TempData["SuccessMessage"] = "Vui lòng kiểm tra email để nhận mã OTP.";
                    return RedirectToAction("VerifyOtp");
                }

                // ✅ FIX 2: Dùng TempData thay vì ModelState.AddModelError
                // vì Register.cshtml đang hiển thị TempData["ErrorMessage"], không phải validation summary
                TempData["ErrorMessage"] = response?.Message ?? "Đăng ký thất bại. Vui lòng thử lại.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                TempData["ErrorMessage"] = "Lỗi kết nối đến server. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        // GET: Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["VerifyEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            
            var model = new VerifyOtpViewModel { Email = email };
            return View(model);
        }

        // POST: Account/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _authService.VerifyOtpAsync(model);

                if (response != null && response.Success && response.Data != null)
                {
                    HttpContext.Session.SetString("JWTToken", response.Data.Token);
                    HttpContext.Session.SetString("UserName", response.Data.FullName);
                    HttpContext.Session.SetString("UserEmail", response.Data.Email);
                    HttpContext.Session.SetString("UserRole", response.Data.Role);
                    HttpContext.Session.SetInt32("UserId", response.Data.UserId);

                    TempData["SuccessMessage"] = "Xác thực thành công! Đăng nhập tự động.";

                    // ✅ Nếu là Admin → chuyển đến Admin Dashboard
                    if (response.Data.Role == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    // User thường → trang chủ
                    return Redirect("/");
                }

                TempData["ErrorMessage"] = response?.Message ?? "Mã OTP không hợp lệ. Vui lòng thử lại.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification");
                TempData["ErrorMessage"] = "Lỗi kết nối đến server. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}
