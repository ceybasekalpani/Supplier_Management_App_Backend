namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DashboardPermissionSettingsDto
    {
        public List<DashboardUserDto> Users { get; set; } = new();
        public List<DashboardModuleDto> Modules { get; set; } = new();
        public Dictionary<int, DashboardUserPermissionDto> UserPermissions { get; set; } = new();
    }

    public class DashboardModuleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DashboardSubPermissionDto> SubPermissions { get; set; } = new();
    }

    public class DashboardSubPermissionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DashboardUserPermissionDto
    {
        public Dictionary<string, bool> ModulePermissions { get; set; } = new();
        public Dictionary<string, List<string>> SubPermissions { get; set; } = new();
    }

    public class SaveDashboardUserPermissionsDto
    {
        public Dictionary<string, bool> ModulePermissions { get; set; } = new();
        public Dictionary<string, List<string>> SubPermissions { get; set; } = new();
    }
}
