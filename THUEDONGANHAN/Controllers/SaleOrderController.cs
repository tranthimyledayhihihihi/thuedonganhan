using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleOrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleOrderController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method để lấy UserId từ JWT token
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        // GET: api/SaleOrder
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetAllSaleOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // ✅ FIX: Thêm pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .OrderByDescending(so => so.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = orders.Select(o => MapToSaleOrderResponse(o)).ToList();

                var result = new
                {
                    items = response,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, 
                    $"Lấy danh sách đơn bán thành công (Trang {page}/{totalPages})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/SaleOrder/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SaleOrderResponse>>> GetSaleOrder(int id)
        {
            try
            {
                var order = await _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<SaleOrderResponse>.ErrorResponse("Không tìm thấy đơn bán"));
                }

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = MapToSaleOrderResponse(order);

                return Ok(ApiResponse<SaleOrderResponse>.SuccessResponse(response, "Lấy thông tin đơn bán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SaleOrderResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/SaleOrder/buyer/5
        [HttpGet("buyer/{buyerId}")]
        public async Task<ActionResult<ApiResponse<List<SaleOrderResponse>>>> GetOrdersByBuyer(int buyerId)
        {
            try
            {
                var orders = await _context.SaleOrders
                    .Include(so => so.Product)
                    .Where(so => so.BuyerId == buyerId)
                    .OrderByDescending(so => so.CreatedAt)
                    .ToListAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = orders.Select(o => MapToSaleOrderResponse(o)).ToList();

                return Ok(ApiResponse<List<SaleOrderResponse>>.SuccessResponse(response, "Lấy lịch sử mua hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<SaleOrderResponse>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/SaleOrder/seller/5
        [HttpGet("seller/{sellerId}")]
        public async Task<ActionResult<ApiResponse<List<SaleOrderResponse>>>> GetOrdersBySeller(int sellerId)
        {
            try
            {
                // ✅ FIX: Lấy đơn bán theo OwnerId của Product (vì không có SellerId trong model)
                var orders = await _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .Where(so => so.Product.OwnerId == sellerId)
                    .OrderByDescending(so => so.CreatedAt)
                    .ToListAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = orders.Select(o => MapToSaleOrderResponse(o)).ToList();

                return Ok(ApiResponse<List<SaleOrderResponse>>.SuccessResponse(response, "Lấy lịch sử bán hàng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<SaleOrderResponse>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/SaleOrder
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SaleOrderResponse>>> CreateSaleOrder([FromBody] SaleOrder order)
        {
            try
            {
                // ✅ LẤY USER ID TỪ JWT
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SaleOrder>.ErrorResponse("Không xác định được người dùng"));
                }

                // Kiểm tra sản phẩm
                var product = await _context.Products
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.ProductId == order.ProductId);

                if (product == null)
                {
                    return NotFound(ApiResponse<SaleOrder>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // ✅ FIX: Kiểm tra ProductType làm nguồn sự thật duy nhất
                if (product.ProductType != "Sale" && product.ProductType != "Both")
                {
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Sản phẩm này không được bán"));
                }

                // ✅ KIỂM TRA: Không mua đồ của chính mình
                if (product.OwnerId == currentUserId.Value)
                {
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Bạn không thể mua sản phẩm của chính mình"));
                }

                // ✅ KIỂM TRA: Sản phẩm có bán không
                if (!product.IsForSale || product.SalePrice == null)
                {
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Sản phẩm này không bán"));
                }

                // ✅ TẠO ĐỚN BÁN
                order.BuyerId = currentUserId.Value;
                order.SalePrice = product.SalePrice.Value; // ✅ FIX: Dùng SalePrice thay vì UnitPrice
                order.Status = "Pending";
                order.CreatedAt = DateTime.UtcNow;

                _context.SaleOrders.Add(order);

                // ✅ TRỪ SỐ LƯỢNG SẢN PHẨM (mặc định mua 1)
                product.Quantity -= 1;
                if (product.Quantity == 0)
                {
                    product.IsAvailable = false;
                    product.IsForSale = false;
                }

                await _context.SaveChangesAsync();

                var createdOrder = await _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == order.SaleOrderId);

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = MapToSaleOrderResponse(createdOrder!);

                return CreatedAtAction(nameof(GetSaleOrder), new { id = order.SaleOrderId },
                    ApiResponse<SaleOrderResponse>.SuccessResponse(response, 
                        $"Đặt mua thành công. Giá: {order.SalePrice:N0} VNĐ"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SaleOrderResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/SaleOrder/5/confirm
        [HttpPut("{id}/confirm")]
        public async Task<ActionResult<ApiResponse<SaleOrderResponse>>> ConfirmOrder(int id)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SaleOrder>.ErrorResponse("Không xác định được người dùng"));
                }

                var order = await _context.SaleOrders
                    .Include(so => so.Product)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<SaleOrder>.ErrorResponse("Không tìm thấy đơn bán"));
                }

                // ✅ CHỈ NGƯỜI BÁN (OwnerId của Product) MỚI ĐƯỢC XÁC NHẬN
                if (order.Product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                order.Status = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = MapToSaleOrderResponse(order);

                return Ok(ApiResponse<SaleOrderResponse>.SuccessResponse(response, "Xác nhận đơn bán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SaleOrderResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/SaleOrder/5/complete
        [HttpPut("{id}/complete")]
        public async Task<ActionResult<ApiResponse<SaleOrderResponse>>> CompleteOrder(int id)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SaleOrder>.ErrorResponse("Không xác định được người dùng"));
                }

                var order = await _context.SaleOrders
                    .Include(so => so.Product)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<SaleOrder>.ErrorResponse("Không tìm thấy đơn bán"));
                }

                // ✅ CHỈ NGƯỜI BÁN (OwnerId của Product) MỚI ĐƯỢC HOÀN THÀNH
                if (order.Product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                order.Status = "Completed";
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = MapToSaleOrderResponse(order);

                return Ok(ApiResponse<SaleOrderResponse>.SuccessResponse(response, "Hoàn thành đơn bán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SaleOrderResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/SaleOrder/5/cancel
        [HttpPut("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<SaleOrderResponse>>> CancelOrder(int id)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SaleOrder>.ErrorResponse("Không xác định được người dùng"));
                }

                var order = await _context.SaleOrders
                    .Include(so => so.Product)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<SaleOrder>.ErrorResponse("Không tìm thấy đơn bán"));
                }

                // ✅ NGƯỜI MUA HOẶC NGƯỜI BÁN (OwnerId của Product) ĐỀU CÓ THỂ HỦY
                if (order.BuyerId != currentUserId.Value && order.Product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                // ✅ CHỈ HỦY ĐƯỢC KHI ĐANG PENDING HOẶC CONFIRMED
                if (order.Status != "Pending" && order.Status != "Confirmed")
                {
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Không thể hủy đơn hàng ở trạng thái này"));
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;

                // ✅ HOÀN LẠI SỐ LƯỢNG SẢN PHẨM
                var product = order.Product;
                product.Quantity += 1; // Mặc định mua 1
                product.IsAvailable = true;

                await _context.SaveChangesAsync();

                // ✅ FIX: Dùng DTO thay vì raw model
                var response = MapToSaleOrderResponse(order);

                return Ok(ApiResponse<SaleOrderResponse>.SuccessResponse(response, "Hủy đơn bán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SaleOrderResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/SaleOrder/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.SaleOrders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy đơn bán"));
                }

                _context.SaleOrders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa đơn bán thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // Helper method để map SaleOrder sang SaleOrderResponse
        private SaleOrderResponse MapToSaleOrderResponse(SaleOrder order)
        {
            return new SaleOrderResponse
            {
                SaleOrderId = order.SaleOrderId,
                ProductId = order.ProductId,
                BuyerId = order.BuyerId,
                SalePrice = order.SalePrice,
                Status = order.Status,
                PreviousOwnerId = order.PreviousOwnerId,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Product = order.Product != null ? new ProductSummary
                {
                    ProductId = order.Product.ProductId,
                    ProductName = order.Product.ProductName,
                    ImageUrl = (!string.IsNullOrEmpty(order.Product.ImageUrl) && order.Product.ImageUrl.StartsWith("/uploads/"))
                        ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{order.Product.ImageUrl}"
                        : order.Product.ImageUrl,
                    Location = order.Product.Location,
                    OwnerId = order.Product.OwnerId
                } : null,
                Buyer = order.Buyer != null ? new UserSummary
                {
                    UserId = order.Buyer.UserId,
                    FullName = order.Buyer.FullName,
                    Email = order.Buyer.Email,
                    PhoneNumber = order.Buyer.PhoneNumber
                } : null
            };
        }
    }
}
