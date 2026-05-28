using System.ComponentModel.DataAnnotations;

namespace THUEDONGANHAN.Helpers
{
    public class UteEmailAttribute : ValidationAttribute
    {
        private const string RequiredDomain = "@sv.ute.udn.vn";
        private const string AdminEmail = "admin@ute.udn.vn";

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Email là bắt buộc");
            }

            string email = value.ToString()!.ToLower().Trim();

            // Cho phép admin email
            if (email == AdminEmail.ToLower())
            {
                return ValidationResult.Success;
            }

            // Kiểm tra email phải có đuôi @sv.ute.udn.vn
            if (!email.EndsWith(RequiredDomain.ToLower()))
            {
                return new ValidationResult($"Email phải có đuôi {RequiredDomain} (ví dụ: 23115053122326{RequiredDomain})");
            }

            return ValidationResult.Success;
        }
    }
}
