using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IDashboardCommunicationService
    {
        Task<CommunicationDashboardDto> GetAllAsync();
        Task<List<DashboardNewsDto>> GetNewsAsync(string? status);
        Task<DashboardNewsDto> CreateNewsAsync(CreateDashboardNewsDto request, string? createdBy);
        Task<DashboardNewsDto?> UpdateNewsAsync(int id, UpdateDashboardNewsDto request);
        Task<bool> DeleteNewsAsync(int id);
        Task<List<DashboardNotificationDto>> GetNotificationsAsync(string? status);
        Task<DashboardNotificationDto> CreateNotificationAsync(CreateDashboardNotificationDto request, string? createdBy);
        Task<DashboardNotificationDto?> UpdateNotificationAsync(int id, UpdateDashboardNotificationDto request);
        Task<bool> DeleteNotificationAsync(int id);
    }
}
