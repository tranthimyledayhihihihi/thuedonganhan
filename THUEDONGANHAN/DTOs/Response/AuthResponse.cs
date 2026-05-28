namespace THUEDONGANHAN.DTOs.Response
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // Renter, Owner, Both
        public string? Token { get; set; } // [v4.0] Nullable - chưa có token khi chưa verify OTP
        public DateTime? ExpiresAt { get; set; } // [v4.0] Nullable
    }
}
