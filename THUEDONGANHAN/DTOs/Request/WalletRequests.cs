using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.DTOs.Request
{
    public class DepositRequest
    {
        [Required]
        [Range(10000, 100000000, ErrorMessage = "Số tiền nạp tối thiểu là 10,000đ và tối đa là 100,000,000đ")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
    }

    public class WithdrawRequest
    {
        [Required]
        [Range(10000, 100000000, ErrorMessage = "Số tiền rút tối thiểu là 10,000đ và tối đa là 100,000,000đ")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
