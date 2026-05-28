using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SeedController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint để thêm dữ liệu mẫu vào database
        /// Truy cập: GET /api/Seed/products
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> SeedProducts()
        {
            try
            {
                // Kiểm tra đã có sản phẩm chưa
                var existingProductCount = await _context.Products.CountAsync();
                if (existingProductCount > 0)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Database đã có sản phẩm rồi!", 
                        count = existingProductCount 
                    });
                }

                // Xóa dữ liệu cũ nếu có (để tránh lỗi duplicate)
                var existingCategories = await _context.Categories.ToListAsync();
                if (existingCategories.Any())
                {
                    _context.Categories.RemoveRange(existingCategories);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra và tạo Categories
                if (!await _context.Categories.AnyAsync())
                {
                    var categories = new List<Category>
                    {
                        new Category { CategoryName = "Điện tử", Description = "Thiết bị điện tử, máy tính, camera", CreatedAt = DateTime.Now },
                        new Category { CategoryName = "Xe cộ", Description = "Xe máy, xe đạp, ô tô", CreatedAt = DateTime.Now },
                        new Category { CategoryName = "Thời trang", Description = "Quần áo, phụ kiện, lễ phục", CreatedAt = DateTime.Now },
                        new Category { CategoryName = "Thể thao", Description = "Dụng cụ thể thao, gym", CreatedAt = DateTime.Now },
                        new Category { CategoryName = "Sách & Học liệu", Description = "Sách giáo trình, tài liệu học tập", CreatedAt = DateTime.Now }
                    };
                    _context.Categories.AddRange(categories);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra và tạo Admin User
                if (!await _context.Users.AnyAsync(u => u.Email == "admin@ute.udn.vn"))
                {
                    var admin = new User
                    {
                        FullName = "Admin UTE",
                        Email = "admin@ute.udn.vn",
                        PhoneNumber = "0123456789",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        Role = "Admin",
                        UserType = "Both",
                        IsActive = true,
                        IsVerified = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Users.Add(admin);
                    await _context.SaveChangesAsync();
                }

                // Lấy AdminId và CategoryIds
                var adminId = (await _context.Users.FirstAsync(u => u.Email == "admin@ute.udn.vn")).UserId;
                var categoryDienTu = (await _context.Categories.FirstAsync(c => c.CategoryName == "Điện tử")).CategoryId;
                var categoryXeCo = (await _context.Categories.FirstAsync(c => c.CategoryName == "Xe cộ")).CategoryId;
                var categoryThoiTrang = (await _context.Categories.FirstAsync(c => c.CategoryName == "Thời trang")).CategoryId;
                var categoryTheThao = (await _context.Categories.FirstAsync(c => c.CategoryName == "Thể thao")).CategoryId;
                var categorySach = (await _context.Categories.FirstAsync(c => c.CategoryName == "Sách & Học liệu")).CategoryId;

                // Tạo danh sách sản phẩm mẫu
                var products = new List<Product>
                {
                    // Điện tử
                    new Product
                    {
                        ProductName = "MacBook Pro 14 inch M3",
                        Description = "MacBook Pro 14 inch chip M3, RAM 16GB, SSD 512GB. Máy mới 99%, đầy đủ phụ kiện. Phù hợp cho sinh viên IT, thiết kế đồ họa.",
                        PricePerDay = 150000,
                        PricePerWeek = 900000,
                        PricePerMonth = 3000000,
                        Deposit = 5000000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryDienTu
                    },
                    new Product
                    {
                        ProductName = "iPad Pro 11 inch 2024",
                        Description = "iPad Pro 11 inch M2, 256GB, WiFi + Cellular. Kèm Apple Pencil Gen 2 và Magic Keyboard. Lý tưởng cho ghi chú, vẽ, học tập.",
                        PricePerDay = 80000,
                        PricePerWeek = 500000,
                        PricePerMonth = 1800000,
                        Deposit = 3000000,
                        Quantity = 1,
                        ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryDienTu
                    },
                    new Product
                    {
                        ProductName = "Camera Canon EOS R6",
                        Description = "Canon EOS R6 Full Frame, kèm lens RF 24-105mm f/4L. Máy chuyên nghiệp cho nhiếp ảnh, quay phim. Đầy đủ phụ kiện, thẻ nhớ 128GB.",
                        PricePerDay = 200000,
                        PricePerWeek = 1200000,
                        PricePerMonth = 4000000,
                        Deposit = 10000000,
                        Quantity = 1,
                        ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryDienTu
                    },
                    new Product
                    {
                        ProductName = "Sony WH-1000XM5 Headphones",
                        Description = "Tai nghe Sony WH-1000XM5, chống ồn chủ động hàng đầu. Pin 30 giờ, âm thanh Hi-Res. Phù hợp học tập, làm việc, giải trí.",
                        PricePerDay = 30000,
                        PricePerWeek = 180000,
                        PricePerMonth = 600000,
                        Deposit = 500000,
                        Quantity = 3,
                        ImageUrl = "https://images.unsplash.com/photo-1545127398-14699f92334b?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryDienTu
                    },
                    
                    // Xe cộ
                    new Product
                    {
                        ProductName = "Xe đạp thể thao Giant ATX 810",
                        Description = "Xe đạp địa hình Giant ATX 810, 27.5 inch, phanh đĩa thủy lực. Xe mới 95%, phù hợp đi học, tập thể dục.",
                        PricePerDay = 20000,
                        PricePerWeek = 120000,
                        PricePerMonth = 400000,
                        Deposit = 1000000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryXeCo
                    },
                    new Product
                    {
                        ProductName = "Xe máy Honda Wave RSX",
                        Description = "Honda Wave RSX 2022, màu đỏ đen, xe đẹp, máy êm. Bảo hiểm đầy đủ. Giao xe tận nơi trong bán kính 5km.",
                        PricePerDay = 50000,
                        PricePerWeek = 300000,
                        PricePerMonth = 1000000,
                        Deposit = 2000000,
                        Quantity = 1,
                        ImageUrl = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryXeCo
                    },
                    
                    // Thời trang
                    new Product
                    {
                        ProductName = "Lễ phục tốt nghiệp UTE",
                        Description = "Lễ phục tốt nghiệp UTE đầy đủ: áo, mũ, cổ. Size M, L, XL. Giặt sạch sẽ, ủi phẳng. Phù hợp cho lễ tốt nghiệp, chụp ảnh kỷ yếu.",
                        PricePerDay = 50000,
                        PricePerWeek = 300000,
                        Deposit = 500000,
                        Quantity = 5,
                        ImageUrl = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryThoiTrang
                    },
                    new Product
                    {
                        ProductName = "Vest nam công sở",
                        Description = "Vest nam công sở cao cấp, màu xanh navy, size M-L. Phù hợp cho phỏng vấn, sự kiện, hội thảo. Kèm cà vạt và giày tây.",
                        PricePerDay = 80000,
                        PricePerWeek = 480000,
                        PricePerMonth = 1500000,
                        Deposit = 1000000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryThoiTrang
                    },
                    
                    // Sách & Học liệu
                    new Product
                    {
                        ProductName = "Sách Giải tích nâng cao",
                        Description = "Sách Giải tích nâng cao dành cho sinh viên kỹ thuật. Sách mới 90%, không viết vẽ. Bao gồm bài tập và lời giải chi tiết.",
                        PricePerDay = 5000,
                        PricePerWeek = 30000,
                        PricePerMonth = 100000,
                        Deposit = 100000,
                        Quantity = 3,
                        ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categorySach
                    },
                    new Product
                    {
                        ProductName = "Bộ sách lập trình C/C++",
                        Description = "Bộ 3 cuốn: Lập trình C cơ bản, C++ nâng cao, Cấu trúc dữ liệu và giải thuật. Sách mới, đầy đủ. Phù hợp cho sinh viên IT.",
                        PricePerDay = 10000,
                        PricePerWeek = 60000,
                        PricePerMonth = 200000,
                        Deposit = 200000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=800&h=600&fit=crop",
                        Location = "Ký túc xá UTE, Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categorySach
                    },
                    
                    // Thể thao
                    new Product
                    {
                        ProductName = "Bộ tạ tay 20kg",
                        Description = "Bộ tạ tay điều chỉnh 20kg (2 thanh x 10kg). Kèm găng tay tập gym. Phù hợp tập tại nhà, ký túc xá.",
                        PricePerDay = 15000,
                        PricePerWeek = 90000,
                        PricePerMonth = 300000,
                        Deposit = 300000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryTheThao
                    },
                    new Product
                    {
                        ProductName = "Vợt cầu lông Yonex",
                        Description = "Vợt cầu lông Yonex Astrox 99 Pro, chính hãng. Kèm bao vợt, cầu lông. Phù hợp cho người chơi trung bình đến nâng cao.",
                        PricePerDay = 20000,
                        PricePerWeek = 120000,
                        PricePerMonth = 400000,
                        Deposit = 500000,
                        Quantity = 2,
                        ImageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800&h=600&fit=crop",
                        Location = "Đà Nẵng",
                        IsAvailable = true,
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        OwnerId = adminId,
                        CategoryId = categoryTheThao
                    }
                };

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "✅ Đã thêm dữ liệu mẫu thành công!",
                    data = new
                    {
                        categories = await _context.Categories.CountAsync(),
                        users = await _context.Users.CountAsync(),
                        products = await _context.Products.CountAsync()
                    },
                    instructions = new
                    {
                        step1 = "Truy cập: http://localhost:5001/Product để xem danh sách sản phẩm",
                        step2 = "Đăng nhập với: admin@ute.udn.vn / Admin@123",
                        step3 = "Hoặc đăng ký tài khoản mới với email @sv.ute.udn.vn"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi thêm dữ liệu mẫu",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Xóa tất cả sản phẩm (để test lại)
        /// Truy cập: DELETE /api/Seed/products
        /// </summary>
        [HttpDelete("products")]
        public async Task<IActionResult> ClearProducts()
        {
            try
            {
                var products = await _context.Products.ToListAsync();
                _context.Products.RemoveRange(products);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Đã xóa tất cả sản phẩm",
                    deletedCount = products.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi xóa sản phẩm",
                    error = ex.Message
                });
            }
        }
    }
}
