using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardAdminAuthService
    {
        Task<DashboardAdminLoginResponseDto?> LoginAsync(string username, string password);
    }
}
