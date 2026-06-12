using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardPermissionService
    {
        Task<DashboardPermissionSettingsDto> GetSettingsAsync();
        Task<DashboardUserPermissionDto?> GetUserPermissionsAsync(int adminId);
        Task<DashboardUserPermissionDto?> SaveUserPermissionsAsync(int adminId, SaveDashboardUserPermissionsDto request);
    }
}
