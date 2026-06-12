using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardUserService
    {
        Task<DashboardUserManagementDto> GetUsersAsync(string? search, string? status);
        Task<DashboardUserDto> CreateUserAsync(CreateDashboardUserDto request);
        Task<DashboardUserDto?> UpdateUserAsync(int id, UpdateDashboardUserDto request);
        Task<bool> DeleteUserAsync(int id, int? currentAdminId);
    }
}
