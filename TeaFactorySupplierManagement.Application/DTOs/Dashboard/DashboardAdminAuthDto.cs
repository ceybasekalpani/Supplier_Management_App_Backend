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
        public bool IsSuperAdmin { get; set; }
        public bool HasPermissionData { get; set; } = true;
        public List<string> Permissions { get; set; } = new();
        public Dictionary<string, bool> ModulePermissions { get; set; } = new();
        public Dictionary<string, List<string>> SubPermissions { get; set; } = new();
    }
}
