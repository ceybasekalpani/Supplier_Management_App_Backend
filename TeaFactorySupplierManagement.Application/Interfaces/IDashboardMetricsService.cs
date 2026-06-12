using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardMetricsService
    {
        Task<DashboardMetricsDto> GetMetricsAsync(int months = 6, int recent = 10);
    }
}
