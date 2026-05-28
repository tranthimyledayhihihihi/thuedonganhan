using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OTPController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OTPController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/OTP/send
        // Gửi OTP xác thực email trường (sau khi đăng ký)
        [HttpPost("send")]
        public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            if (user.IsVerified)
                return BadRequest(new { message = "Tài khoản đã được xác thực" });

            if (user.Student == null)
                return BadRequest(new { message = "Tài khoản chưa liên kết với sinh viên" });

            // Tạo mã OTP 6 số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Lưu OTP vào database (hết hạn sau 10 phút)
            var otp = new OTPVerification
            {
                UserId = user.UserId,
                OTPCode = otpCode,
                OTPType = request.OTPType ?? "EmailVerification",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.OTPVerifications.Add(otp);
            await _context.SaveChangesAsync();

            // TODO: Gửi email thực tế qua SMTP
            // await SendEmailAsync(user.Student.Email, "Mã xác thực OTP", $"Mã OTP của bạn là: {otpCode}");

            // ⚠️ CHỈ DÙNG CHO DEV/TEST - XÓA DÒNG NÀY KHI PRODUCTION
            var response = new
            {
                message = $"Mã OTP đã được gửi đến email {user.Student.Email}",
                expiresAt = otp.ExpiresAt,
                email = user.Student.Email
            };

            // Chỉ trả về OTP code trong development mode
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return Ok(new
                {
                    response.message,
                    otpCode = otpCode, // ⚠️ CHỈ HIỂN THỊ TRONG DEV MODE
                    response.expiresAt,
                    response.email
                });
            }

            return Ok(response);
        }

        // POST: api/OTP/verify
        // Xác thực OTP
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            if (user.IsVerified)
                return BadRequest(new { message = "Tài khoản đã được xác thực" });

            // Lấy OTP mới nhất chưa sử dụng
            var otp = await _context.OTPVerifications
                .Where(o => o.UserId == request.UserId
                         && o.OTPType == (request.OTPType ?? "EmailVerification")
                         && o.IsUsed == false)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return BadRequest(new { message = "Không tìm thấy OTP hợp lệ" });

            // Kiểm tra số lần nhập sai
            if (otp.AttemptCount >= 5)
                return BadRequest(new { message = "OTP đã bị khóa do nhập sai quá 5 lần" });

            // Kiểm tra hết hạn
            if (DateTime.UtcNow > otp.ExpiresAt)
                return BadRequest(new { message = "OTP đã hết hạn" });

            // Tăng số lần thử
            otp.AttemptCount++;
            await _context.SaveChangesAsync();

            // Kiểm tra mã OTP
            if (otp.OTPCode != request.OTPCode)
            {
                return BadRequest(new
                {
                    message = "Mã OTP không đúng",
                    attemptsLeft = 5 - otp.AttemptCount
                });
            }

            // Xác thực thành công
            otp.IsUsed = true;
            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xác thực thành công",
                isVerified = true
            });
        }

        // GET: api/OTP/status/{userId}
        // Kiểm tra trạng thái xác thực
        [HttpGet("status/{userId}")]
        public async Task<IActionResult> GetVerificationStatus(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(new
            {
                userId = user.UserId,
                email = user.Email,
                isVerified = user.IsVerified,
                studentCode = user.Student?.StudentCode,
                studentEmail = user.Student?.Email
            });
        }

        // POST: api/OTP/resend
        // Gửi lại OTP
        [HttpPost("resend")]
        public async Task<IActionResult> ResendOTP([FromBody] SendOTPRequest request)
        {
            // Kiểm tra OTP cũ còn hạn không
            var existingOTP = await _context.OTPVerifications
                .Where(o => o.UserId == request.UserId
                         && o.OTPType == (request.OTPType ?? "EmailVerification")
                         && o.IsUsed == false
                         && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOTP != null)
            {
                var timeLeft = (existingOTP.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                if (timeLeft > 540) // Còn hơn 9 phút (chỉ cho phép resend sau 1 phút)
                {
                    return BadRequest(new
                    {
                        message = "Vui lòng đợi ít nhất 1 phút trước khi gửi lại OTP",
                        secondsLeft = (int)timeLeft - 540
                    });
                }
            }

            // Gửi OTP mới
            return await SendOTP(request);
        }
    }

    // DTOs
    public class SendOTPRequest
    {
        public int UserId { get; set; }
        public string? OTPType { get; set; } = "EmailVerification";
    }

    public class VerifyOTPRequest
    {
        public int UserId { get; set; }
        public string OTPCode { get; set; } = string.Empty;
        public string? OTPType { get; set; } = "EmailVerification";
    }
}
