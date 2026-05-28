using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THUEDONGANHAN.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        public int? ProductId { get; set; }

        public int? RentalId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;

        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("RentalId")]
        public virtual Rental? Rental { get; set; }
    }
}
