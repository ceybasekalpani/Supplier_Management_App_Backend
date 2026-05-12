using TeaFactorySupplierManagement.Application.DTOs.Requests;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ICashRequestService
{
    Task<List<CashRequestDto>> GetRequestsByRegNoAsync(int regNo);
    Task<CashRequestDto?> GetRequestByIdAsync(int id);
    Task<CashRequestDto> CreateRequestAsync(int regNo, CreateCashRequestDto request);
    Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null);
    Task<bool> DeleteRequestAsync(int id);
    Task<bool> IsCashRequestEnabledAsync();
    Task<decimal?> GetAdvanceLimitAsync();
}
}