using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardUserService
    {
        Task<DashboardUserManagementDto> GetUsersAsync(string? search, string? status);
        Task<DashboardUserDto> CreateUserAsync(CreateDashboardUserDto request);
        Task<DashboardUserDto?> UpdateUserAsync(int id, UpdateDashboardUserDto request);
        Task<string?> GetUserAvatarAsync(int id);
        Task<DashboardUserDto?> UpdateUserAvatarAsync(int id, string avatarUrl);
        Task<DashboardUserDto?> ClearUserAvatarAsync(int id);
        Task<bool> DeleteUserAsync(int id, int? currentAdminId);
    }
}
