using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using THUEDONGANHAN.Data;
using THUEDONGANHAN.DTOs.Response;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // ✅ KHÓA TOÀN BỘ CHỈ CHO ADMIN TRUY CẬP
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Student
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetAllStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 200) pageSize = 200;

                var query = _context.Students.AsQueryable();

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var students = await query
                    .OrderBy(s => s.StudentCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    items = students,
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
                    $"Lấy danh sách sinh viên thành công (Trang {page}/{totalPages})"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Student/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Student>>> GetStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound(ApiResponse<Student>.ErrorResponse("Không tìm thấy sinh viên"));
                }

                return Ok(ApiResponse<Student>.SuccessResponse(student, "Lấy thông tin sinh viên thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Student>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Student/code/{studentCode}
        [HttpGet("code/{studentCode}")]
        public async Task<ActionResult<ApiResponse<Student>>> GetStudentByCode(string studentCode)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == studentCode);

                if (student == null)
                {
                    return NotFound(ApiResponse<Student>.ErrorResponse("Không tìm thấy sinh viên"));
                }

                return Ok(ApiResponse<Student>.SuccessResponse(student, "Lấy thông tin sinh viên thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Student>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Student/search?keyword=nguyen
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<Student>>>> SearchStudents([FromQuery] string keyword)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.StudentCode.Contains(keyword) 
                        || s.FullName.Contains(keyword) 
                        || s.Email.Contains(keyword))
                    .Take(50)
                    .ToListAsync();

                return Ok(ApiResponse<List<Student>>.SuccessResponse(students, 
                    $"Tìm thấy {students.Count} sinh viên"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<Student>>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Student
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Student>>> CreateStudent([FromBody] Student student)
        {
            try
            {
                // Kiểm tra trùng mã sinh viên
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == student.StudentCode);

                if (existingStudent != null)
                {
                    return BadRequest(ApiResponse<Student>.ErrorResponse("Mã sinh viên đã tồn tại"));
                }

                // Kiểm tra trùng email
                var existingEmail = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == student.Email);

                if (existingEmail != null)
                {
                    return BadRequest(ApiResponse<Student>.ErrorResponse("Email đã tồn tại"));
                }

                student.CreatedAt = DateTime.Now;
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetStudent), new { id = student.StudentId },
                    ApiResponse<Student>.SuccessResponse(student, "Thêm sinh viên thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Student>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // POST: api/Student/import-csv
        [HttpPost("import-csv")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> ImportStudentsFromCsv(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Không có file được chọn"));
                }

                // ✅ KIỂM TRA LOẠI FILE
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".csv")
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Chỉ chấp nhận file .csv"));
                }

                // ✅ KIỂM TRA KÍCH THƯỚC (tối đa 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File không được vượt quá 10MB"));
                }

                var students = new List<Student>();
                var errors = new List<string>();
                var lineNumber = 0;

                using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
                {
                    // ✅ ĐỌC HEADER
                    var header = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(header))
                    {
                        return BadRequest(ApiResponse<object>.ErrorResponse("File CSV rỗng"));
                    }

                    lineNumber++;

                    // ✅ ĐỌC TỪNG DÒNG
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        lineNumber++;

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue; // Bỏ qua dòng trống
                        }

                        try
                        {
                            var values = line.Split(',');

                            if (values.Length < 3)
                            {
                                errors.Add($"Dòng {lineNumber}: Thiếu dữ liệu (cần ít nhất StudentCode, FullName, Email)");
                                continue;
                            }

                            var studentCode = values[0].Trim();
                            var fullName = values[1].Trim();
                            var email = values[2].Trim();
                            var phoneNumber = values.Length > 3 ? values[3].Trim() : null;
                            var major = values.Length > 4 ? values[4].Trim() : null;
                            var faculty = values.Length > 5 ? values[5].Trim() : null;
                            var classValue = values.Length > 6 ? values[6].Trim() : null;
                            var academicYear = values.Length > 7 && int.TryParse(values[7].Trim(), out int year) ? year : (int?)null;

                            // ✅ VALIDATION
                            if (string.IsNullOrEmpty(studentCode))
                            {
                                errors.Add($"Dòng {lineNumber}: Mã sinh viên không được để trống");
                                continue;
                            }

                            if (string.IsNullOrEmpty(fullName))
                            {
                                errors.Add($"Dòng {lineNumber}: Họ tên không được để trống");
                                continue;
                            }

                            if (string.IsNullOrEmpty(email))
                            {
                                errors.Add($"Dòng {lineNumber}: Email không được để trống");
                                continue;
                            }

                            // ✅ KIỂM TRA TRÙNG TRONG DATABASE
                            var existingStudent = await _context.Students
                                .FirstOrDefaultAsync(s => s.StudentCode == studentCode);

                            if (existingStudent != null)
                            {
                                errors.Add($"Dòng {lineNumber}: Mã sinh viên '{studentCode}' đã tồn tại");
                                continue;
                            }

                            var existingEmail = await _context.Students
                                .FirstOrDefaultAsync(s => s.Email == email);

                            if (existingEmail != null)
                            {
                                errors.Add($"Dòng {lineNumber}: Email '{email}' đã tồn tại");
                                continue;
                            }

                            // ✅ KIỂM TRA TRÙNG TRONG DANH SÁCH IMPORT
                            if (students.Any(s => s.StudentCode == studentCode))
                            {
                                errors.Add($"Dòng {lineNumber}: Mã sinh viên '{studentCode}' bị trùng trong file");
                                continue;
                            }

                            if (students.Any(s => s.Email == email))
                            {
                                errors.Add($"Dòng {lineNumber}: Email '{email}' bị trùng trong file");
                                continue;
                            }

                            // ✅ TẠO STUDENT
                            var student = new Student
                            {
                                StudentCode = studentCode,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phoneNumber,
                                Major = major,
                                Faculty = faculty,
                                Class = classValue,
                                AcademicYear = academicYear,
                                Status = "Active",
                                CreatedAt = DateTime.Now
                            };

                            students.Add(student);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Dòng {lineNumber}: Lỗi xử lý - {ex.Message}");
                        }
                    }
                }

                // ✅ LƯU VÀO DATABASE
                if (students.Any())
                {
                    _context.Students.AddRange(students);
                    await _context.SaveChangesAsync();
                }

                var result = new
                {
                    totalLines = lineNumber - 1, // Trừ header
                    successCount = students.Count,
                    errorCount = errors.Count,
                    errors = errors.Take(50).ToList() // Chỉ trả về 50 lỗi đầu tiên
                };

                var message = $"Import thành công {students.Count}/{lineNumber - 1} sinh viên";
                if (errors.Any())
                {
                    message += $". Có {errors.Count} lỗi.";
                }

                return Ok(ApiResponse<object>.SuccessResponse(result, message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // GET: api/Student/export-template
        [HttpGet("export-template")]
        public IActionResult ExportCsvTemplate()
        {
            try
            {
                // ✅ TẠO FILE CSV MẪU
                var csv = new StringBuilder();
                csv.AppendLine("StudentCode,FullName,Email,PhoneNumber,Major,Faculty,Class,AcademicYear");
                csv.AppendLine("20T1234567,Nguyen Van A,20t1234567@ute.udn.vn,0905123456,Công nghệ thông tin,Công nghệ,20T1,2020");
                csv.AppendLine("21T7654321,Tran Thi B,21t7654321@ute.udn.vn,0905654321,Kỹ thuật phần mềm,Công nghệ,21T2,2021");
                csv.AppendLine("22T1111111,Le Van C,22t1111111@ute.udn.vn,0905111111,An toàn thông tin,Công nghệ,22T1,2022");

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", "student_template.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Lỗi server: {ex.Message}" });
            }
        }

        // PUT: api/Student/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Student>>> UpdateStudent(int id, [FromBody] Student student)
        {
            try
            {
                if (id != student.StudentId)
                {
                    return BadRequest(ApiResponse<Student>.ErrorResponse("ID không khớp"));
                }

                var existingStudent = await _context.Students.FindAsync(id);
                if (existingStudent == null)
                {
                    return NotFound(ApiResponse<Student>.ErrorResponse("Không tìm thấy sinh viên"));
                }

                existingStudent.FullName = student.FullName;
                existingStudent.Email = student.Email;
                existingStudent.PhoneNumber = student.PhoneNumber;
                existingStudent.Major = student.Major;
                existingStudent.Faculty = student.Faculty;
                existingStudent.Class = student.Class;
                existingStudent.AcademicYear = student.AcademicYear;
                existingStudent.Status = student.Status;
                existingStudent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Student>.SuccessResponse(existingStudent, "Cập nhật sinh viên thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Student>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }

        // DELETE: api/Student/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy sinh viên"));
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa sinh viên thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Lỗi server: {ex.Message}"));
            }
        }
    }
}
