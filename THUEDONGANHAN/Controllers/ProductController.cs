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
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Helper method để lưu base64 image
        private async Task<string?> SaveBase64ImageAsync(string? base64Image)
        {
            if (string.IsNullOrEmpty(base64Image) || !base64Image.StartsWith("data:image/"))
            {
                return base64Image; // Không phải base64 hoặc rỗng, trả về nguyên bản
            }

            try
            {
                var parts = base64Image.Split(',');
                if (parts.Length != 2) return base64Image;

                var header = parts[0];
                var base64Data = parts[1];

                var extension = ".jpg";
                if (header.Contains("image/png")) extension = ".png";
                else if (header.Contains("image/gif")) extension = ".gif";
                else if (header.Contains("image/webp")) extension = ".webp";

                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var filePath = Path.Combine(uploadDir, fileName);
                var bytes = Convert.FromBase64String(base64Data);
                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return $"{baseUrl}/uploads/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductController] Error saving base64 image: {ex.Message}");
                return null;
            }
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

        // GET: api/Product
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetAllProducts(
            [FromQuery] int? currentUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // ✅ VALIDATION
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 items/page

                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => p.IsAvailable && p.IsApproved);

                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    query = query.Where(p => p.OwnerId != currentUserId.Value);
                }

                // ✅ ĐẾM TỔNG SỐ
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // ✅ LẤY DỮ LIỆU THEO TRANG
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = products.Select(MapToProductResponse).ToList();

                // ✅ TRẢ VỀ KÈM PAGINATION INFO
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
                    $"Lấy danh sách sản phẩm thành công (Trang {page}/{totalPages})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<ProductResponse>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                return Ok(ApiResponse<ProductResponse>.SuccessResponse(MapToProductResponse(product), "Lấy thông tin sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Product/search?keyword=xe
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> SearchProducts([FromQuery] string keyword)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => (p.ProductName.Contains(keyword) || p.Description.Contains(keyword)) && p.IsAvailable && p.IsApproved)
                    .ToListAsync();

                var response = products.Select(MapToProductResponse).ToList();
                return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response, $"Tìm thấy {response.Count} sản phẩm"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProductResponse>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Product/filter?categoryIds=1,2&minPrice=0&maxPrice=100000&sortBy=price_asc&page=1&pageSize=10&productType=Rent
        [HttpGet("filter")]
        public async Task<ActionResult<ApiResponse<object>>> FilterProducts(
            [FromQuery] string? categoryIds,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] string? productType, // ✅ THÊM FILTER THEO LOẠI (Rent | Sale | Both)
            [FromQuery] string? keyword, // ✅ THÊM LỌC TỪ KHÓA
            [FromQuery] int? currentUserId, // ✅ THÊM ĐỂ LOẠI BỎ SẢN PHẨM CỦA CHÍNH MÌNH
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // ✅ VALIDATION
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => p.IsAvailable && p.IsApproved)
                    .AsQueryable();

                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    query = query.Where(p => p.OwnerId != currentUserId.Value);
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(p => p.ProductName.Contains(keyword) || p.Description.Contains(keyword));
                }

                if (!string.IsNullOrEmpty(categoryIds))
                {
                    try
                    {
                        var categoryIdList = categoryIds.Split(',')
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => int.Parse(s.Trim()))
                            .ToList();
                        
                        if (categoryIdList.Any())
                        {
                            query = query.Where(p => categoryIdList.Contains(p.CategoryId));
                        }
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ApiResponse<object>.ErrorResponse($"Invalid categoryIds format: {ex.Message}"));
                    }
                }

                if (minPrice.HasValue)
                {
                    query = query.Where(p => p.PricePerDay >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.PricePerDay <= maxPrice.Value);
                }

                // ✅ FILTER THEO PRODUCT TYPE
                if (!string.IsNullOrEmpty(productType))
                {
                    if (productType == "Rent")
                    {
                        query = query.Where(p => p.ProductType == "Rent" || p.ProductType == "Both");
                    }
                    else if (productType == "Sale")
                    {
                        query = query.Where(p => p.ProductType == "Sale" || p.ProductType == "Both");
                    }
                    else if (productType == "Both")
                    {
                        query = query.Where(p => p.ProductType == "Both");
                    }
                }

                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.PricePerDay),
                    "price_desc" => query.OrderByDescending(p => p.PricePerDay),
                    "name_asc" => query.OrderBy(p => p.ProductName),
                    "name_desc" => query.OrderByDescending(p => p.ProductName),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    "rating" => query.OrderByDescending(p => p.AverageRating),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                // ✅ PAGINATION
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = products.Select(MapToProductResponse).ToList();

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
                    $"Tìm thấy {totalItems} sản phẩm (Trang {page}/{totalPages})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Product/category/1
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => p.CategoryId == categoryId && p.IsAvailable && p.IsApproved)
                    .ToListAsync();

                var response = products.Select(MapToProductResponse).ToList();
                return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response, "Lấy sản phẩm theo danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProductResponse>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Product/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetProductsByUserId(int userId)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .Where(p => p.OwnerId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var response = products.Select(MapToProductResponse).ToList();
                return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response, $"Lấy {response.Count} sản phẩm của user thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProductResponse>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Product
        [HttpPost]
        [Authorize] // ✅ BẮT BUỘC ĐĂNG NHẬP
        public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                // ✅ LẤY USER ID TỪ JWT TOKEN
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ProductResponse>.ErrorResponse("Không xác định được người dùng"));
                }

                Console.WriteLine($"[ProductController] Received CreateProduct request");
                Console.WriteLine($"[ProductController] ProductName: {request.ProductName}");
                Console.WriteLine($"[ProductController] CurrentUserId from JWT: {currentUserId}");
                Console.WriteLine($"[ProductController] CategoryId: {request.CategoryId}");
                Console.WriteLine($"[ProductController] PricePerDay: {request.PricePerDay}");
                Console.WriteLine($"[ProductController] PricePerHour: {request.PricePerHour}");
                Console.WriteLine($"[ProductController] UnavailableDates: {request.UnavailableDates}");

                if (string.IsNullOrEmpty(request.ProductName))
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Tên sản phẩm không được để trống"));
                }

                if (request.PricePerDay <= 0)
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Giá thuê theo ngày phải lớn hơn 0"));
                }

                if (request.Deposit < 0)
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Tiền cọc không được là số âm"));
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Số lượng sản phẩm phải lớn hơn 0"));
                }

                if (request.PricePerHour.HasValue && request.PricePerHour < 0) return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Giá thuê theo giờ không hợp lệ"));
                if (request.PricePerWeek.HasValue && request.PricePerWeek < 0) return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Giá thuê theo tuần không hợp lệ"));
                if (request.PricePerMonth.HasValue && request.PricePerMonth < 0) return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Giá thuê theo tháng không hợp lệ"));
                
                if (request.IsForSale && (!request.SalePrice.HasValue || request.SalePrice.Value <= 0))
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Giá bán phải lớn hơn 0 nếu cho phép bán"));
                }

                if (request.CategoryId <= 0)
                {
                    return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Danh mục không hợp lệ"));
                }

                // ✅ DÙNG USER ID TỪ TOKEN, KHÔNG DÙNG TỪ REQUEST
                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    PricePerHour = (request.PricePerHour == 0) ? null : request.PricePerHour,
                    PricePerDay = request.PricePerDay,
                    PricePerWeek = (request.PricePerWeek == 0) ? null : request.PricePerWeek,
                    PricePerMonth = (request.PricePerMonth == 0) ? null : request.PricePerMonth,
                    Deposit = request.Deposit ?? 0,
                    Quantity = request.Quantity,
                    AvailableQuantity = request.Quantity, // ✅ Đồng bộ số lượng sẵn có
                    ImageUrl = await SaveBase64ImageAsync(request.ImageUrl),
                    Location = request.Location,
                    CategoryId = request.CategoryId,
                    OwnerId = currentUserId.Value, // Dùng User ID từ JWT
                    UnavailableDates = request.UnavailableDates,
                    CreatedAt = DateTime.Now,
                    IsAvailable = true,
                    IsApproved = false,
                    ProductType = request.ProductType,
                    IsForSale = request.IsForSale,
                    SalePrice = (request.SalePrice == 0) ? null : request.SalePrice
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (request.ProductImages != null && request.ProductImages.Any())
                {
                    int sortOrder = 0;
                    foreach (var base64Img in request.ProductImages)
                    {
                        var imgPath = await SaveBase64ImageAsync(base64Img);
                        if (!string.IsNullOrEmpty(imgPath))
                        {
                            var productImage = new ProductImage
                            {
                                ProductId = product.ProductId,
                                ImageUrl = imgPath,
                                SortOrder = sortOrder++,
                                CreatedAt = DateTime.Now
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine($"[ProductController] Product created successfully with ID: {product.ProductId}");

                var createdProduct = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, 
                    ApiResponse<ProductResponse>.SuccessResponse(MapToProductResponse(createdProduct!), "Đăng sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductController] Error creating product: {ex.Message}");
                Console.WriteLine($"[ProductController] StackTrace: {ex.StackTrace}");
                return StatusCode(500, ApiResponse<ProductResponse>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        [Authorize] // ✅ ĐÃ CÓ AUTHORIZE
        public async Task<ActionResult<ApiResponse<Product>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<Product>.ErrorResponse("Không xác định được người dùng"));
                }

                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound(ApiResponse<Product>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // ✅ CHỈ CHỦ SỞ HỮU MỚI ĐƯỢC SỬA
                if (existingProduct.OwnerId != currentUserId.Value)
                {
                    return Forbid(); // 403 Forbidden
                }

                // ✅ FIX: Cập nhật các luật xác thực cho Edit
                if (string.IsNullOrEmpty(request.ProductName)) return BadRequest(ApiResponse<Product>.ErrorResponse("Tên sản phẩm không được để trống"));
                if (request.PricePerDay <= 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Giá thuê theo ngày phải lớn hơn 0"));
                if (request.Deposit < 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Tiền cọc không được là số âm"));
                if (request.Quantity <= 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Số lượng sản phẩm phải lớn hơn 0"));
                if (request.PricePerHour.HasValue && request.PricePerHour < 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Giá thuê theo giờ không hợp lệ"));
                if (request.PricePerWeek.HasValue && request.PricePerWeek < 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Giá thuê theo tuần không hợp lệ"));
                if (request.PricePerMonth.HasValue && request.PricePerMonth < 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Giá thuê theo tháng không hợp lệ"));
                if (request.IsForSale && (!request.SalePrice.HasValue || request.SalePrice.Value <= 0)) return BadRequest(ApiResponse<Product>.ErrorResponse("Giá bán phải lớn hơn 0 nếu cho phép bán"));
                if (request.CategoryId <= 0) return BadRequest(ApiResponse<Product>.ErrorResponse("Danh mục không hợp lệ"));

                // ✅ FIX: Chỉ cập nhật các trường được phép từ DTO
                // Không cho phép thay đổi OwnerId, CreatedAt, các counter
                existingProduct.ProductName = request.ProductName;
                existingProduct.Description = request.Description;
                existingProduct.PricePerHour = (request.PricePerHour == 0) ? null : request.PricePerHour;
                existingProduct.PricePerDay = request.PricePerDay;
                existingProduct.PricePerWeek = (request.PricePerWeek == 0) ? null : request.PricePerWeek;
                existingProduct.PricePerMonth = (request.PricePerMonth == 0) ? null : request.PricePerMonth;
                existingProduct.Deposit = request.Deposit ?? 0;
                existingProduct.Quantity = request.Quantity;
                existingProduct.AvailableQuantity = request.Quantity; // ✅ Đồng bộ số lượng sẵn có khi cập nhật
                
                // Nếu có ảnh mới (base64) thì cập nhật
                var newImageUrl = await SaveBase64ImageAsync(request.ImageUrl);
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    existingProduct.ImageUrl = newImageUrl;
                }
                
                existingProduct.Location = request.Location;
                existingProduct.CategoryId = request.CategoryId;
                existingProduct.UnavailableDates = request.UnavailableDates;
                existingProduct.ProductType = request.ProductType;
                existingProduct.IsForSale = request.IsForSale;
                existingProduct.SalePrice = (request.SalePrice == 0) ? null : request.SalePrice;
                existingProduct.UpdatedAt = DateTime.Now;

                // Cập nhật danh sách hình ảnh phụ (ProductImages) nếu có
                if (request.ProductImages != null)
                {
                    var oldImages = await _context.ProductImages.Where(pi => pi.ProductId == id).ToListAsync();
                    _context.ProductImages.RemoveRange(oldImages);

                    int sortOrder = 0;
                    foreach (var img in request.ProductImages)
                    {
                        var imgPath = await SaveBase64ImageAsync(img);
                        if (!string.IsNullOrEmpty(imgPath))
                        {
                            var productImage = new ProductImage
                            {
                                ProductId = id,
                                ImageUrl = imgPath,
                                SortOrder = sortOrder++,
                                CreatedAt = DateTime.Now
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Product>.SuccessResponse(existingProduct, "Cập nhật sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Product>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Không xác định được người dùng"));

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy sản phẩm"));

                if (product.OwnerId != currentUserId.Value)
                    return Forbid();

                // ✅ BUG #14 FIX: Không cho xóa sản phẩm có đơn thuê đang hoạt động
                var activeRentals = await _context.Rentals
                    .AnyAsync(r => r.ProductId == id &&
                        (r.Status == "Pending" || r.Status == "Confirmed" ||
                         r.Status == "InProgress" || r.Status == "Active" || r.Status == "Returned"));

                if (activeRentals)
                    return BadRequest(ApiResponse<bool>.ErrorResponse(
                        "Không thể xóa sản phẩm đang có đơn thuê hoạt động. Vui lòng chờ các đơn thuê kết thúc."));

                // Kiểm tra đơn mua đang hoạt động
                var activeSaleOrders = await _context.SaleOrders
                    .AnyAsync(so => so.ProductId == id &&
                        (so.Status == "Pending" || so.Status == "Confirmed"));

                if (activeSaleOrders)
                    return BadRequest(ApiResponse<bool>.ErrorResponse(
                        "Không thể xóa sản phẩm đang có đơn mua chưa hoàn tất."));

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa sản phẩm thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // Helper method để map Product sang ProductResponse (tránh circular reference)
        private ProductResponse MapToProductResponse(Product p)
        {
            return new ProductResponse
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                PricePerHour = p.PricePerHour,
                PricePerDay = p.PricePerDay,
                PricePerWeek = p.PricePerWeek,
                PricePerMonth = p.PricePerMonth,
                Deposit = p.Deposit,
                // ✅ THÊM CÁC TRƯỜNG MỚI
                ProductType = p.ProductType,
                IsForSale = p.IsForSale,
                SalePrice = p.SalePrice,
                Quantity = p.Quantity,
                ImageUrl = (!string.IsNullOrEmpty(p.ImageUrl) && p.ImageUrl.StartsWith("/uploads/"))
                            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{p.ImageUrl}"
                            : p.ImageUrl,
                Location = p.Location,
                UnavailableDates = p.UnavailableDates,
                IsAvailable = p.IsAvailable,
                IsApproved = p.IsApproved,
                CreatedAt = p.CreatedAt,
                OwnerId = p.OwnerId,
                CategoryId = p.CategoryId,
                Category = p.Category != null ? new CategoryResponse
                {
                    CategoryId = p.Category.CategoryId,
                    CategoryName = p.Category.CategoryName,
                    Description = p.Category.Description
                } : null,
                Owner = p.Owner != null ? new OwnerResponse
                {
                    UserId = p.Owner.UserId,
                    FullName = p.Owner.FullName,
                    Email = p.Owner.Email,
                    PhoneNumber = p.Owner.PhoneNumber
                } : null,
                ProductImages = p.ProductImages != null
                    ? p.ProductImages.OrderBy(pi => pi.SortOrder).Select(pi =>
                        (!string.IsNullOrEmpty(pi.ImageUrl) && pi.ImageUrl.StartsWith("/uploads/"))
                            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{pi.ImageUrl}"
                            : pi.ImageUrl
                    ).ToList()
                    : new List<string>()
            };
        }
    }
}
