using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.DTOs.Request
{
    public class SendMessageRequest
    {
        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public int? ProductId { get; set; }
        public int? RentalId { get; set; }
    }
}
