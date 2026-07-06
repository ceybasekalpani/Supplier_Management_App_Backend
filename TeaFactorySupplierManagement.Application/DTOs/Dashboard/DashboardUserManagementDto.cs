namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DashboardUserSummaryDto
    {
        public int TotalAdministrators { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }

    public class DashboardUserManagementDto
    {
        public DashboardUserSummaryDto Summary { get; set; } = new();
        public List<DashboardUserDto> Users { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    public class DashboardUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ProfileImage { get; set; }
        public bool IsSuperAdmin { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? LastLoginAt { get; set; }
         public string? UpdatedAt { get; set; }
    }

    public class CreateDashboardUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNo { get; set; }
        public string Role { get; set; } = "Admin";
        public string Status { get; set; } = "active";
        public string? Avatar { get; set; }
    }

    public class UpdateDashboardUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? PhoneNo { get; set; }
        public string Role { get; set; } = "Admin";
        public string Status { get; set; } = "active";
        public string? Avatar { get; set; }
    }
}
