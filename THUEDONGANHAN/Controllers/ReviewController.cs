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
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Review/product/5
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<ApiResponse<List<Review>>>> GetReviewsByProduct(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Review>>.SuccessResponse(reviews, "Lấy danh sách đánh giá thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Review>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Review/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<Review>>>> GetReviewsByUser(int userId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return Ok(ApiResponse<List<Review>>.SuccessResponse(reviews, "Lấy lịch sử đánh giá thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Review>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Review
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<Review>>> CreateReview([FromBody] Review review)
        {
            try
            {
                var product = await _context.Products.FindAsync(review.ProductId);
                if (product == null)
                    return NotFound(ApiResponse<Review>.ErrorResponse("Không tìm thấy sản phẩm"));

                var user = await _context.Users.FindAsync(review.UserId);
                if (user == null)
                    return NotFound(ApiResponse<Review>.ErrorResponse("Không tìm thấy người dùng"));

                // Kiểm tra user có đơn thuê đã hoàn thành với sản phẩm này không
                var hasCompletedRental = await _context.Rentals
                    .AnyAsync(r => r.ProductId == review.ProductId
                        && r.RenterId == review.UserId
                        && r.Status == "Completed");

                if (!hasCompletedRental)
                    return BadRequest(ApiResponse<Review>.ErrorResponse(
                        "Bạn chỉ có thể đánh giá sản phẩm sau khi hoàn thành đơn thuê"));

                // Kiểm tra user đã đánh giá sản phẩm này chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == review.ProductId && r.UserId == review.UserId);

                if (existingReview != null)
                    return BadRequest(ApiResponse<Review>.ErrorResponse("Bạn đã đánh giá sản phẩm này rồi"));

                review.CreatedAt = DateTime.Now;
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // ✅ FIX: Cast double → decimal khi tính Average
                var allReviews = await _context.Reviews
                    .Where(r => r.ProductId == review.ProductId)
                    .ToListAsync();

                product.ReviewCount = allReviews.Count;
                product.AverageRating = (decimal)allReviews.Average(r => r.Rating); // ✅ dòng 114

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReviewsByProduct), new { productId = review.ProductId },
                    ApiResponse<Review>.SuccessResponse(review,
                        $"Đánh giá thành công. Rating trung bình: {product.AverageRating:F1}/5"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Review>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Review/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(int id)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy đánh giá"));

                var productId = review.ProductId;
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    var remainingReviews = await _context.Reviews
                        .Where(r => r.ProductId == productId)
                        .ToListAsync();

                    product.ReviewCount = remainingReviews.Count;
                    product.AverageRating = remainingReviews.Any()
                        ? (decimal)remainingReviews.Average(r => r.Rating) // ✅ dòng 155
                        : 0;

                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa đánh giá thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
