using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDisbursementService
    {
        Task<DisbursementQueueDto> GetApprovedQueueAsync(string? route, DateTime? fromDate, DateTime? toDate);
        Task<DisbursementTrackingDto?> IssueAsync(string issuedType, int requestId, string? method, string? issuedBy);
        Task<List<DisbursementTrackingDto>> GetTrackingAsync(string? issuedType, string? status, string? search, DateTime? fromDate, DateTime? toDate);
        Task<DisbursementTrackingDto?> MarkReceivedAsync(int id, string? completedBy, string? completedDevice);
    }
}
