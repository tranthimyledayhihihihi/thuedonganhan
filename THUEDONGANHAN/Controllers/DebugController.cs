using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DebugController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Test BCrypt hash và verify
        /// </summary>
        [HttpGet("test-bcrypt")]
        public IActionResult TestBCrypt([FromQuery] string password = "Admin@123")
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var verify = BCrypt.Net.BCrypt.Verify(password, hash);

            return Ok(new
            {
                password = password,
                hash = hash,
                verify = verify,
                hashLength = hash.Length
            });
        }

        /// <summary>
        /// Kiểm tra admin trong database
        /// </summary>
        [HttpGet("check-admin")]
        public async Task<IActionResult> CheckAdmin()
        {
            var admin = await _context.Users
                .Where(u => u.Email == "admin@ute.udn.vn")
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.UserType,
                    u.IsActive,
                    u.IsVerified,
                    PasswordHashLength = u.PasswordHash.Length,
                    PasswordHashPrefix = u.PasswordHash.Substring(0, Math.Min(20, u.PasswordHash.Length))
                })
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                return NotFound(new { message = "Admin không tồn tại" });
            }

            return Ok(admin);
        }

        /// <summary>
        /// Test verify password với admin
        /// </summary>
        [HttpPost("test-admin-password")]
        public async Task<IActionResult> TestAdminPassword([FromQuery] string password = "Admin@123")
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@ute.udn.vn");

            if (admin == null)
            {
                return NotFound(new { message = "Admin không tồn tại" });
            }

            var isValid = BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);

            return Ok(new
            {
                email = admin.Email,
                passwordTested = password,
                isValid = isValid,
                passwordHashLength = admin.PasswordHash.Length,
                passwordHashPrefix = admin.PasswordHash.Substring(0, Math.Min(20, admin.PasswordHash.Length))
            });
        }

        /// <summary>
        /// Reset password cho admin
        /// </summary>
        [HttpPost("reset-admin-password")]
        public async Task<IActionResult> ResetAdminPassword([FromQuery] string newPassword = "Admin@123")
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@ute.udn.vn");

            if (admin == null)
            {
                return NotFound(new { message = "Admin không tồn tại" });
            }

            var oldHash = admin.PasswordHash;
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            admin.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Test verify ngay
            var isValid = BCrypt.Net.BCrypt.Verify(newPassword, admin.PasswordHash);

            return Ok(new
            {
                message = "Đã reset password thành công",
                email = admin.Email,
                newPassword = newPassword,
                oldHashPrefix = oldHash.Substring(0, Math.Min(20, oldHash.Length)),
                newHashPrefix = admin.PasswordHash.Substring(0, Math.Min(20, admin.PasswordHash.Length)),
                verifyTest = isValid
            });
        }
    }
}
