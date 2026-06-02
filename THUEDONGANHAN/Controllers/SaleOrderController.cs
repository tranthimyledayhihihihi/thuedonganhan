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
        private const decimal COMMISSION_RATE = 0.05m;

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
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var currentUserId = GetCurrentUserId();
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                // ✅ BUG #9 FIX: Chỉ Admin xem tất cả, user thường chỉ xem đơn của mình
                var query = _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .AsQueryable();

                if (role != "Admin" && currentUserId.HasValue)
                    query = query.Where(so => so.BuyerId == currentUserId.Value
                                          || so.Product.OwnerId == currentUserId.Value);

                query = query.OrderByDescending(so => so.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

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

                // ✅ TẠO ĐƠN BÁN & GIAO DỊCH VÍ TRONG DB TRANSACTION
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var buyer = await _context.Users.FindAsync(currentUserId.Value);
                    if (buyer == null)
                    {
                        return NotFound(ApiResponse<SaleOrder>.ErrorResponse("Người mua không tồn tại"));
                    }

                    // ✅ BUG #10 FIX: Chặn tài khoản bị ban mua hàng
                    if (!buyer.IsActive)
                        return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Admin."));
                    if (!buyer.IsVerified)
                        return BadRequest(ApiResponse<SaleOrder>.ErrorResponse("Tài khoản chưa xác thực email trường."));

                    if (buyer.Balance < product.SalePrice.Value)
                    {
                        return BadRequest(ApiResponse<SaleOrder>.ErrorResponse($"Số dư không đủ. Cần {product.SalePrice.Value:N0}đ, số dư hiện tại: {buyer.Balance:N0}đ."));
                    }

                    // 1. Trừ tiền ví người mua
                    buyer.Balance -= product.SalePrice.Value;

                    // 2. Tạo đơn bán
                    order.BuyerId = currentUserId.Value;
                    order.SalePrice = product.SalePrice.Value;
                    order.Status = "Pending";
                    order.CreatedAt = DateTime.UtcNow;

                    _context.SaleOrders.Add(order);
                    await _context.SaveChangesAsync(); // Lưu để có SaleOrderId

                    // 3. Ghi lịch sử giao dịch trừ tiền
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = buyer.UserId,
                        Type = "Payment",
                        Amount = -order.SalePrice,
                        ReferenceId = order.SaleOrderId,
                        Description = $"Thanh toán mua sản phẩm #{product.ProductName} (Đơn #{order.SaleOrderId})",
                        CreatedAt = DateTime.Now
                    });

                    // 4. Trừ số lượng sản phẩm (mặc định mua 1)
                    product.Quantity -= 1;
                    if (product.Quantity == 0)
                    {
                        product.IsAvailable = false;
                        product.IsForSale = false;
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }

                var createdOrder = await _context.SaleOrders
                    .Include(so => so.Product)
                    .Include(so => so.Buyer)
                    .FirstOrDefaultAsync(so => so.SaleOrderId == order.SaleOrderId);

                // ✅ Tạo thông báo cho người bán
                if (createdOrder != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = product.OwnerId,
                        Title = "Đơn mua hàng mới",
                        Body = $"Sinh viên {createdOrder.Buyer.FullName} đã đặt mua sản phẩm '{product.ProductName}' của bạn.",
                        Type = "SaleOrderCreated",
                        ReferenceId = order.SaleOrderId,
                        ReferenceType = "Product",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

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

                // ✅ BUG #11 FIX: Chỉ xác nhận khi đơn đang Pending
                if (order.Status != "Pending")
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse(
                        $"Không thể xác nhận đơn ở trạng thái '{order.Status}'. Chỉ xác nhận đơn đang 'Pending'."));

                order.Status = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                // ✅ Tạo thông báo cho người mua
                _context.Notifications.Add(new Notification
                {
                    UserId = order.BuyerId,
                    Title = "Đơn mua hàng được xác nhận",
                    Body = $"Người bán đã xác nhận đơn hàng mua sản phẩm '{order.Product.ProductName}' của bạn.",
                    Type = "SaleOrderConfirmed",
                    ReferenceId = order.SaleOrderId,
                    ReferenceType = "Product",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

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

                // ✅ BUG #12 FIX: Chỉ hoàn thành khi đơn đang Confirmed
                if (order.Status != "Confirmed")
                    return BadRequest(ApiResponse<SaleOrder>.ErrorResponse(
                        $"Chỉ có thể hoàn thành đơn khi ở trạng thái 'Confirmed'. Trạng thái hiện tại: {order.Status}"));

                // ✅ HOÀN THÀNH ĐƠN BÁN & CHUYỂN TIỀN TRONG DB TRANSACTION
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    decimal commission = Math.Round(order.SalePrice * COMMISSION_RATE, 2);
                    decimal sellerPayout = order.SalePrice - commission;

                    // 1. Cộng tiền cho người bán (đã trừ 5% hoa hồng)
                    var seller = await _context.Users.FindAsync(order.Product.OwnerId);
                    if (seller != null)
                    {
                        seller.Balance += sellerPayout;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = seller.UserId,
                            Type = "Payment",
                            Amount = sellerPayout,
                            ReferenceId = order.SaleOrderId,
                            Description = $"Nhận tiền bán sản phẩm #{order.Product.ProductName} (Đơn #{order.SaleOrderId} - 5% hoa hồng: -{commission:N0}đ = +{sellerPayout:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // 2. Cộng 5% hoa hồng cho ví Admin
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser != null)
                    {
                        adminUser.Balance += commission;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = adminUser.UserId,
                            Type = "Commission",
                            Amount = commission,
                            ReferenceId = order.SaleOrderId,
                            Description = $"Hoa hồng 5% từ đơn bán #{order.SaleOrderId} (SP: {order.Product.ProductName}) +{commission:N0}đ",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // 3. Cập nhật trạng thái đơn
                    order.Status = "Completed";
                    order.UpdatedAt = DateTime.Now;

                    // 4. Tạo thông báo cho người mua
                    _context.Notifications.Add(new Notification
                    {
                        UserId = order.BuyerId,
                        Title = "Đơn mua hàng hoàn thành",
                        Body = $"Đơn mua sản phẩm '{order.Product.ProductName}' đã hoàn thành. Sản phẩm hiện thuộc sở hữu của bạn.",
                        Type = "SaleCompleted",
                        ReferenceId = order.SaleOrderId,
                        ReferenceType = "Product",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }

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

                // ✅ HỦY ĐƠN & HOÀN TIỀN TRONG DB TRANSACTION
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Hoàn lại 100% tiền cho người mua
                    var buyer = await _context.Users.FindAsync(order.BuyerId);
                    if (buyer != null)
                    {
                        buyer.Balance += order.SalePrice;
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = buyer.UserId,
                            Type = "Refund",
                            Amount = order.SalePrice,
                            ReferenceId = order.SaleOrderId,
                            Description = $"Hoàn tiền hủy đơn mua #{order.SaleOrderId} (+{order.SalePrice:N0}đ)",
                            CreatedAt = DateTime.Now
                        });
                    }

                    // 2. Cập nhật trạng thái
                    order.Status = "Cancelled";
                    order.UpdatedAt = DateTime.Now;

                    // 3. Hoàn lại số lượng sản phẩm vào kho
                    var product = order.Product;
                    product.Quantity += 1; // Mặc định mua 1
                    product.IsAvailable = true;

                    // 4. Tạo thông báo cho đối phương
                    int targetNotifyUserId = (currentUserId.Value == order.BuyerId) ? order.Product.OwnerId : order.BuyerId;
                    string notifierRoleName = (currentUserId.Value == order.BuyerId) ? "Người mua" : "Người bán";

                    _context.Notifications.Add(new Notification
                    {
                        UserId = targetNotifyUserId,
                        Title = "Đơn mua hàng đã bị hủy",
                        Body = $"{notifierRoleName} đã hủy đơn mua #{order.SaleOrderId} cho sản phẩm '{product.ProductName}'. Tiền đã được hoàn.",
                        Type = "RentalCancelled", // Dùng type này vì constraint CK_Notifications_Type
                        ReferenceId = order.SaleOrderId,
                        ReferenceType = "Product",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }

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
