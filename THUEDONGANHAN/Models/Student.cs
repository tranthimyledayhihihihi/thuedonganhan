using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ✅ FIX: Thêm namespace cho [Column]

namespace THUEDONGANHAN.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string StudentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? Major { get; set; }

        [MaxLength(100)]
        public string? Faculty { get; set; } // Khoa

        [MaxLength(50)]
        [Column("ClassName")] // ✅ FIX: Map đúng tên cột trong database
        public string? Class { get; set; } // Lớp: 23T1, 22T2...

        public int? AcademicYear { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active | Graduated | Suspended | LeaveOfAbsence

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
