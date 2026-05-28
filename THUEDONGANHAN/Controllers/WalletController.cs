using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Request;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WalletController> _logger;

        public WalletController(AppDbContext context, ILogger<WalletController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        // GET: api/Wallet/balance
        [HttpGet("balance")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetBalance()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<decimal>.ErrorResponse("Không xác định được người dùng"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(ApiResponse<decimal>.ErrorResponse("Người dùng không tồn tại"));
                }

                return Ok(ApiResponse<decimal>.SuccessResponse(user.Balance, "Lấy số dư tài khoản thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // GET: api/Wallet/transactions
        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<List<Transaction>>>> GetTransactions()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<List<Transaction>>.ErrorResponse("Không xác định được người dùng"));
                }

                var transactions = await _context.Transactions
                    .Where(t => t.UserId == userId.Value)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Transaction>>.SuccessResponse(transactions, "Lấy lịch sử giao dịch thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Transaction>>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // POST: api/Wallet/deposit
        [HttpPost("deposit")]
        public async Task<ActionResult<ApiResponse<decimal>>> Deposit([FromBody] DepositRequest request)
        {
            _logger.LogInformation("=== DEPOSIT REQUEST RECEIVED ===");
            _logger.LogInformation($"Amount: {request.Amount}, Description: {request.Description}");
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"User ID from token: {userId}");
                
                if (userId == null)
                {
                    _logger.LogWarning("User ID is null - Unauthorized");
                    return Unauthorized(ApiResponse<decimal>.ErrorResponse("Không xác định được người dùng"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(ApiResponse<decimal>.ErrorResponse("Người dùng không tồn tại"));
                }

                _logger.LogInformation($"User found: {user.Email}, Current Balance: {user.Balance}");

                // Validate amount
                if (request.Amount < 10000 || request.Amount > 100000000)
                {
                    _logger.LogWarning($"Invalid amount: {request.Amount}");
                    return BadRequest(ApiResponse<decimal>.ErrorResponse("Số tiền nạp phải từ 10,000đ đến 100,000,000đ"));
                }

                // Thực hiện nạp tiền
                var oldBalance = user.Balance;
                user.Balance += request.Amount;
                user.UpdatedAt = DateTime.Now;

                _logger.LogInformation($"Balance updated: {oldBalance} -> {user.Balance}");

                // Lưu giao dịch
                var transactionRecord = new Transaction
                {
                    UserId = user.UserId,
                    Type = "Deposit",
                    Amount = request.Amount,
                    Description = request.Description ?? $"Nạp tiền vào tài khoản: +{request.Amount:N0}đ",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transactionRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("=== DEPOSIT SUCCESS ===");
                return Ok(ApiResponse<decimal>.SuccessResponse(user.Balance, $"Nạp thành công {request.Amount:N0}đ vào ví. Số dư mới: {user.Balance:N0}đ"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"=== DEPOSIT ERROR === {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        // POST: api/Wallet/withdraw
        [HttpPost("withdraw")]
        public async Task<ActionResult<ApiResponse<decimal>>> Withdraw([FromBody] WithdrawRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<decimal>.ErrorResponse("Không xác định được người dùng"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(ApiResponse<decimal>.ErrorResponse("Người dùng không tồn tại"));
                }

                // Validate amount
                if (request.Amount < 10000 || request.Amount > 100000000)
                {
                    return BadRequest(ApiResponse<decimal>.ErrorResponse("Số tiền rút phải từ 10,000đ đến 100,000,000đ"));
                }

                if (user.Balance < request.Amount)
                {
                    return BadRequest(ApiResponse<decimal>.ErrorResponse("Số dư tài khoản không đủ để thực hiện giao dịch"));
                }

                // Thực hiện rút tiền
                user.Balance -= request.Amount;
                user.UpdatedAt = DateTime.Now;

                // Lưu giao dịch
                var transactionRecord = new Transaction
                {
                    UserId = user.UserId,
                    Type = "Withdraw",
                    Amount = -request.Amount,
                    Description = request.Description ?? $"Rút tiền từ ví tài khoản: -{request.Amount:N0}đ",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transactionRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<decimal>.SuccessResponse(user.Balance, $"Rút thành công {request.Amount:N0}đ từ ví. Số dư mới: {user.Balance:N0}đ"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}
