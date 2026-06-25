using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDisbursementService
    {
        Task<DisbursementQueueDto> GetApprovedQueueAsync(string? route, DateTime? fromDate, DateTime? toDate);
        Task<DisbursementTrackingDto?> IssueAsync(string issuedType, int requestId, string? method, string? issuedBy);
        Task<List<DisbursementTrackingDto>> GetTrackingAsync(string? issuedType, string? status, string? search, DateTime? fromDate, DateTime? toDate);
        Task<DisbursementTrackingDto?> MarkReceivedAsync(int id, string? completedBy, string? completedDevice);
        Task<DeliveryNoteDto> GenerateDeliveryNoteAsync(GenerateDeliveryNoteDto request, int? createdBy);
        Task<List<DeliveryNoteTrackingDto>> GetDeliveryNotesAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate);
        Task<DeliveryNoteDto?> GetDeliveryNoteAsync(int id);
        Task<DeliveryNoteDto?> MarkDeliveryNoteReturnedAsync(int id, string? remarks);
        Task<DeliveryNoteDto?> MarkDeliveryNoteCompletedAsync(int id, string? remarks);
        Task<string?> BuildDeliveryNotePrintHtmlAsync(int id);
    }
}
