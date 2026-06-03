namespace THUEDONGANHAN.DTOs.Request
{
    public class LockUserRequest
    {
        public string Duration { get; set; } = "permanent"; // 1week | 1month | permanent
        public string? Reason { get; set; }
    }
}
