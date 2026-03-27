using TeaFactorySupplierManagement.Application.DTOs.Requests;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IItemRequestService
    {
        Task<List<ItemRequestDto>> GetRequestsByRegNoAsync(string regNo);
        Task<ItemRequestDto?> GetRequestByIdAsync(int id);
        Task<ItemRequestDto> CreateRequestAsync(string regNo, CreateItemRequestDto request);
        Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null);
        Task<bool> DeleteRequestAsync(int id);
        Task<bool> IsItemRequestEnabledAsync();
        Task<List<string>> GetItemTypesAsync();
    }
}