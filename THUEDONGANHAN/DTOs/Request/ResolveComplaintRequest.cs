using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.DTOs.Request
{
    public class ResolveComplaintRequest
    {
        [Required]
        public decimal OwnerCompensation { get; set; }

        [Required]
        public decimal RenterRefund { get; set; }
        
        [Required]
        public string AdminNotes { get; set; } = string.Empty;
    }
}
