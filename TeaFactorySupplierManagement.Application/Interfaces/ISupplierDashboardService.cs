using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ISupplierDashboardService
    {
        Task<SupplierDashboardResponseDto> GetSuppliersAsync(string? search, int months, bool activeOnly);
        Task<DashboardSupplierDto?> GetSupplierAsync(int regNo, int months);
    }
}
