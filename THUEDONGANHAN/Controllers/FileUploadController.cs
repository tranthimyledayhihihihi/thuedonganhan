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
    public class FileUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public FileUploadController(AppDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
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

        // POST: api/FileUpload/image
        [HttpPost("image")]
        public async Task<ActionResult<ApiResponse<string>>> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<string>.ErrorResponse("Không có file được chọn"));
                }

                // ✅ KIỂM TRA LOẠI FILE
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(ApiResponse<string>.ErrorResponse(
                        "Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .gif, .webp"));
                }

                // ✅ KIỂM TRA KÍCH THƯỚC (tối đa 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<string>.ErrorResponse("File không được vượt quá 5MB"));
                }

                // ✅ TẠO THỦ MỤC LƯU FILE
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // ✅ TẠO TÊN FILE DUY NHẤT
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // ✅ LƯU FILE
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ TRẢ VỀ URL
                var fileUrl = $"/uploads/images/{uniqueFileName}";

                return Ok(ApiResponse<string>.SuccessResponse(fileUrl, "Upload ảnh thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Lỗi upload: {ex.Message}"));
            }
        }

        // POST: api/FileUpload/product-images/{productId}
        [HttpPost("product-images/{productId}")]
        public async Task<ActionResult<ApiResponse<List<ProductImage>>>> UploadProductImages(
            int productId, 
            List<IFormFile> files)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<ProductImage>>.ErrorResponse("Không xác định được người dùng"));
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(ApiResponse<List<ProductImage>>.ErrorResponse("Không tìm thấy sản phẩm"));
                }

                // ✅ CHỈ CHỦ SẢN PHẨM MỚI ĐƯỢC UPLOAD
                if (product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                if (files == null || files.Count == 0)
                {
                    return BadRequest(ApiResponse<List<ProductImage>>.ErrorResponse("Không có file được chọn"));
                }

                // ✅ GIỚI HẠN SỐ LƯỢNG ẢNH (tối đa 10 ảnh)
                if (files.Count > 10)
                {
                    return BadRequest(ApiResponse<List<ProductImage>>.ErrorResponse("Tối đa 10 ảnh cho mỗi sản phẩm"));
                }

                var uploadedImages = new List<ProductImage>();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "images");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // ✅ LẤY SỐ THỨ TỰ HIỆN TẠI
                var currentMaxOrder = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId)
                    .MaxAsync(pi => (int?)pi.SortOrder) ?? 0;

                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        continue; // Bỏ qua file không hợp lệ
                    }

                    if (file.Length > 5 * 1024 * 1024)
                    {
                        continue; // Bỏ qua file quá lớn
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileUrl = $"/uploads/images/{uniqueFileName}";
                    currentMaxOrder++;

                    var productImage = new ProductImage
                    {
                        ProductId = productId,
                        ImageUrl = fileUrl,
                        SortOrder = currentMaxOrder,
                        IsPrimary = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.ProductImages.Add(productImage);
                    uploadedImages.Add(productImage);
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<List<ProductImage>>.SuccessResponse(
                    uploadedImages, 
                    $"Upload thành công {uploadedImages.Count} ảnh"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProductImage>>.ErrorResponse($"Lỗi upload: {ex.Message}"));
            }
        }

        // GET: api/FileUpload/product-images/{productId}
        [HttpGet("product-images/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<ProductImage>>>> GetProductImages(int productId)
        {
            try
            {
                var images = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId)
                    .OrderBy(pi => pi.SortOrder)
                    .ToListAsync();

                return Ok(ApiResponse<List<ProductImage>>.SuccessResponse(images, "Lấy danh sách ảnh thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProductImage>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/FileUpload/product-images/{imageId}/set-primary
        [HttpPut("product-images/{imageId}/set-primary")]
        public async Task<ActionResult<ApiResponse<ProductImage>>> SetPrimaryImage(int imageId)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ProductImage>.ErrorResponse("Không xác định được người dùng"));
                }

                var image = await _context.ProductImages
                    .Include(pi => pi.Product)
                    .FirstOrDefaultAsync(pi => pi.ImageId == imageId);

                if (image == null)
                {
                    return NotFound(ApiResponse<ProductImage>.ErrorResponse("Không tìm thấy ảnh"));
                }

                // ✅ CHỈ CHỦ SẢN PHẨM MỚI ĐƯỢC ĐẶT ẢNH CHÍNH
                if (image.Product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                // ✅ BỎ PRIMARY CỦA CÁC ẢNH KHÁC
                var otherImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == image.ProductId && pi.ImageId != imageId)
                    .ToListAsync();

                foreach (var img in otherImages)
                {
                    img.IsPrimary = false;
                }

                // ✅ ĐẶT ẢNH NÀY LÀM PRIMARY
                image.IsPrimary = true;

                // ✅ CẬP NHẬT IMAGEURL CỦA PRODUCT
                image.Product.ImageUrl = image.ImageUrl;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<ProductImage>.SuccessResponse(image, "Đặt ảnh chính thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ProductImage>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/FileUpload/product-images/{imageId}
        [HttpDelete("product-images/{imageId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProductImage(int imageId)
        {
            try
            {
                // ✅ KIỂM TRA OWNERSHIP
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Không xác định được người dùng"));
                }

                var image = await _context.ProductImages
                    .Include(pi => pi.Product)
                    .FirstOrDefaultAsync(pi => pi.ImageId == imageId);

                if (image == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy ảnh"));
                }

                // ✅ CHỈ CHỦ SẢN PHẨM MỚI ĐƯỢC XÓA
                if (image.Product.OwnerId != currentUserId.Value)
                {
                    return Forbid();
                }

                // ✅ XÓA FILE VẬT LÝ
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa ảnh thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
