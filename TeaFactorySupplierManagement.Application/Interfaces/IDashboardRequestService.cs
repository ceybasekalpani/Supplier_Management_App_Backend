using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardRequestService
    {
        Task<DashboardRequestsResponseDto> GetRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate);
        Task<DashboardRequestRowDto?> UpdateRequestStatusAsync(string requestType, int id, string status, string? remarks, string? reviewedBy);
    }
}
