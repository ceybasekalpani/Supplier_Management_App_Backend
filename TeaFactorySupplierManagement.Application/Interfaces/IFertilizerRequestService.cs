using TeaFactorySupplierManagement.Application.DTOs.Requests;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IFertilizerRequestService
    {
        Task<List<FertilizerRequestDto>> GetRequestsByRegNoAsync(int regNo);
        Task<FertilizerRequestDto?> GetRequestByIdAsync(int id);
        Task<FertilizerRequestDto> CreateRequestAsync(int regNo, CreateFertilizerRequestDto request);
        Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null);
        Task<bool> DeleteRequestAsync(int id);
        Task<bool> IsFertilizerRequestEnabledAsync();
        Task<List<string>> GetFertilizerTypesAsync();
    }
}