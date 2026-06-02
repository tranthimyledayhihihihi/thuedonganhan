using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.DTOs.Request
{
    public class CreateComplaintRequest
    {
        [Required(ErrorMessage = "Rental ID là bắt buộc.")]
        public int RentalId { get; set; }

        [Required(ErrorMessage = "Lý do khiếu nại là bắt buộc.")]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề khiếu nại là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả khiếu nại là bắt buộc.")]
        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }
}
