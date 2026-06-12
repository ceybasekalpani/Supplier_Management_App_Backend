namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DashboardAdminLoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class DashboardAdminLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int AdminId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
