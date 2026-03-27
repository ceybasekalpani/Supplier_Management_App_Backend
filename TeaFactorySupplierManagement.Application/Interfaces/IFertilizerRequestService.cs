using TeaFactorySupplierManagement.Application.DTOs.Requests;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IFertilizerRequestService
    {
        Task<List<FertilizerRequestDto>> GetRequestsByRegNoAsync(string regNo);
        Task<FertilizerRequestDto?> GetRequestByIdAsync(int id);
        Task<FertilizerRequestDto> CreateRequestAsync(string regNo, CreateFertilizerRequestDto request);
        Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null);
        Task<bool> DeleteRequestAsync(int id);
        Task<bool> IsFertilizerRequestEnabledAsync();
        Task<List<string>> GetFertilizerTypesAsync();
    }
}