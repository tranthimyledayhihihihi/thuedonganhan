using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Payment/rental/5
        [HttpGet("rental/{rentalId}")]
        public async Task<ActionResult<ApiResponse<List<Payment>>>> GetPaymentsByRental(int rentalId)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Rental)
                    .Where(p => p.RentalId == rentalId)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Ok(ApiResponse<List<Payment>>.SuccessResponse(payments, "Lấy lịch sử thanh toán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Payment>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Payment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Payment>>> GetPayment(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Rental)
                    .FirstOrDefaultAsync(p => p.PaymentId == id);

                if (payment == null)
                {
                    return NotFound(ApiResponse<Payment>.ErrorResponse("Không tìm thấy thanh toán"));
                }

                return Ok(ApiResponse<Payment>.SuccessResponse(payment, "Lấy thông tin thanh toán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Payment>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Payment
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Payment>>> CreatePayment([FromBody] Payment payment)
        {
            try
            {
                // Kiểm tra đơn thuê có tồn tại không
                var rental = await _context.Rentals.FindAsync(payment.RentalId);
                if (rental == null)
                {
                    return NotFound(ApiResponse<Payment>.ErrorResponse("Không tìm thấy đơn thuê"));
                }

                payment.PaymentDate = DateTime.Now;
                payment.PaymentStatus = "Pending";

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId },
                    ApiResponse<Payment>.SuccessResponse(payment, "Tạo thanh toán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Payment>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Payment/5/confirm
        [HttpPut("{id}/confirm")]
        public async Task<ActionResult<ApiResponse<Payment>>> ConfirmPayment(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound(ApiResponse<Payment>.ErrorResponse("Không tìm thấy thanh toán"));
                }

                payment.PaymentStatus = "Completed";
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Payment>.SuccessResponse(payment, "Xác nhận thanh toán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Payment>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Payment/5/refund
        [HttpPut("{id}/refund")]
        public async Task<ActionResult<ApiResponse<Payment>>> RefundPayment(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound(ApiResponse<Payment>.ErrorResponse("Không tìm thấy thanh toán"));
                }

                payment.PaymentStatus = "Refunded";
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Payment>.SuccessResponse(payment, "Hoàn tiền thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Payment>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
