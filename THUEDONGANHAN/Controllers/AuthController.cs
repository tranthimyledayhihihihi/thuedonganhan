using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Request;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Helpers;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment environment, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // [v4.0] Kiểm tra email phải đúng domain trường
                if (!request.Email.EndsWith("@sv.ute.udn.vn", StringComparison.OrdinalIgnoreCase) 
                    && !request.Email.Equals("admin@ute.udn.vn", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Email phải là @sv.ute.udn.vn (sinh viên) hoặc @ute.udn.vn (admin)"));
                }

                // [v4.0] Kiểm tra MSSV và email có trong danh sách sinh viên
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == request.StudentCode 
                                           && s.Email == request.Email 
                                           && s.Status == "Active");

                if (student == null)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(
                        "MSSV hoặc email không tồn tại trong hệ thống trường, hoặc tài khoản sinh viên không còn hoạt động."));
                }

                // [v4.0] Dọn dẹp tài khoản nháp chưa xác thực OTP để cho phép user đăng ký lại
                var draftUsers = await _context.Users
                    .Where(u => !u.IsVerified && 
                               (u.Email == request.Email || 
                                u.PhoneNumber == request.PhoneNumber || 
                                u.StudentId == student.StudentId))
                    .ToListAsync();
                
                if (draftUsers.Any())
                {
                    _context.Users.RemoveRange(draftUsers);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra email đã tồn tại (sau khi dọn dẹp)
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Email đã được sử dụng"));
                }

                // Kiểm tra số điện thoại đã tồn tại
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Số điện thoại đã được sử dụng"));
                }

                // [v4.0] Kiểm tra MSSV đã được đăng ký chưa
                if (await _context.Users.AnyAsync(u => u.StudentId == student.StudentId))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(
                        "MSSV này đã được dùng để đăng ký tài khoản."));
                }

                // [v4.0] Tạo user mới với IsVerified = false (cần xác thực OTP)
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "User",
                    UserType = request.UserType ?? "Both", // Renter | Owner | Both
                    IsActive = true,
                    IsVerified = false, // [v4.0] Cần xác thực OTP
                    StudentId = student.StudentId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // [v4.0] Tạo OTP và gửi email
                var random = new Random();
                var otpCode = random.Next(100000, 999999).ToString();

                var otp = new OTPVerification
                {
                    UserId = user.UserId,
                    OTPCode = otpCode,
                    OTPType = "EmailVerification",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false,
                    AttemptCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OTPVerifications.Add(otp);
                await _context.SaveChangesAsync();

                // Gửi email thực tế qua SMTP
                var subject = "Mã xác thực OTP - Thuê Đồ Ngắn Hạn UTE";
                var htmlMessage = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #2563eb; text-align: center;'>Thuê Đồ Ngắn Hạn UTE</h2>
                        <p>Xin chào <strong>{student.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất việc xác thực email, vui lòng sử dụng mã OTP sau:</p>
                        <div style='background-color: #f3f4f6; padding: 15px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                            <h1 style='margin: 0; color: #1f2937; letter-spacing: 5px;'>{otpCode}</h1>
                        </div>
                        <p>Mã OTP này sẽ hết hạn sau <strong>10 phút</strong>.</p>
                        <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;' />
                        <p style='font-size: 12px; color: #6b7280; text-align: center;'>Hệ thống Thuê Đồ Ngắn Hạn - Đại học Sư phạm Kỹ thuật</p>
                    </div>";

                await _emailService.SendEmailAsync(student.Email, subject, htmlMessage);

                // ⚠️ CHỈ DÙNG CHO DEV/TEST - XÓA DÒNG NÀY KHI PRODUCTION
                var message = _environment.IsDevelopment() 
                    ? $"Đăng ký thành công! Mã OTP đã được gửi đến {student.Email}. OTP: {otpCode} (hết hạn sau 10 phút)"
                    : $"Đăng ký thành công! Mã OTP đã được gửi đến {student.Email} (hết hạn sau 10 phút)";

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(
                    new AuthResponse
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role,
                        UserType = user.UserType,
                        Token = null, // Chưa có token, cần xác thực OTP trước
                        ExpiresAt = null
                    },
                    message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Email hoặc mật khẩu không đúng"));
                }

                if (!user.IsActive)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã bị khóa"));
                }

                // ✅ FIX: Kiểm tra IsVerified (chỉ bắt buộc trong production)
                if (!user.IsVerified && !_environment.IsDevelopment())
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Tài khoản chưa xác thực email. Vui lòng xác thực OTP trước khi đăng nhập."));
                }

                var jwtHelper = new JwtHelper(_configuration);
                var token = jwtHelper.GenerateToken(user);

                var response = new AuthResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    UserType = user.UserType,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!))
                };

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Tài khoản không tồn tại."));
                }

                if (user.IsVerified)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã được xác thực trước đó."));
                }

                // Lấy OTP hợp lệ
                var otp = await _context.OTPVerifications
                    .Where(o => o.UserId == user.UserId 
                             && o.OTPType == "EmailVerification" 
                             && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Không tìm thấy mã OTP hoặc mã đã được sử dụng."));
                }

                if (otp.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới."));
                }

                if (otp.OTPCode != request.OTPCode)
                {
                    otp.AttemptCount++;
                    await _context.SaveChangesAsync();
                    return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Mã OTP không chính xác."));
                }

                // Đánh dấu OTP đã dùng và User đã xác thực
                otp.IsUsed = true;
                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Tạo Token trả về
                var jwtHelper = new JwtHelper(_configuration);
                var token = jwtHelper.GenerateToken(user);

                var response = new AuthResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    UserType = user.UserType,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!))
                };

                return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Xác thực thành công. Bạn đã được đăng nhập."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuthResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // ⚠️ TEMPORARY FIX: Endpoint để verify tài khoản cũ (XÓA SAU KHI MIGRATION)
        [HttpPost("quick-verify/{userId}")]
        public async Task<ActionResult<ApiResponse<string>>> QuickVerify(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy user"));
                }

                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse("OK", $"Đã verify user {user.Email}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }

        // ⚠️ TEMPORARY FIX: Verify tất cả user cũ (XÓA SAU KHI MIGRATION)
        [HttpPost("verify-all-old-users")]
        public async Task<ActionResult<ApiResponse<string>>> VerifyAllOldUsers()
        {
            try
            {
                var unverifiedUsers = await _context.Users
                    .Where(u => !u.IsVerified)
                    .ToListAsync();

                foreach (var user in unverifiedUsers)
                {
                    user.IsVerified = true;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse("OK", 
                    $"Đã verify {unverifiedUsers.Count} tài khoản cũ"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Lỗi: {ex.Message}"));
            }
        }
    }
}
