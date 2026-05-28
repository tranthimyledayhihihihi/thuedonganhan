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
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Category
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Category>>>> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                return Ok(ApiResponse<List<Category>>.SuccessResponse(categories, "Lấy danh sách danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Category>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Category/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Category>>> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return NotFound(ApiResponse<Category>.ErrorResponse("Không tìm thấy danh mục"));
                }

                return Ok(ApiResponse<Category>.SuccessResponse(category, "Lấy thông tin danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Category>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Category
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Category>>> CreateCategory([FromBody] Category category)
        {
            try
            {
                category.CreatedAt = DateTime.Now;
                category.IsActive = true;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId },
                    ApiResponse<Category>.SuccessResponse(category, "Tạo danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Category>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // PUT: api/Category/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Category>>> UpdateCategory(int id, [FromBody] Category category)
        {
            try
            {
                if (id != category.CategoryId)
                {
                    return BadRequest(ApiResponse<Category>.ErrorResponse("ID không khớp"));
                }

                var existingCategory = await _context.Categories.FindAsync(id);
                if (existingCategory == null)
                {
                    return NotFound(ApiResponse<Category>.ErrorResponse("Không tìm thấy danh mục"));
                }

                existingCategory.CategoryName = category.CategoryName;
                existingCategory.Description = category.Description;
                existingCategory.IsActive = category.IsActive;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Category>.SuccessResponse(existingCategory, "Cập nhật danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Category>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Category/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy danh mục"));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa danh mục thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
