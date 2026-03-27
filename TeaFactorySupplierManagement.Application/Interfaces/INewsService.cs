using TeaFactorySupplierManagement.Application.DTOs.Notifications;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface INewsService
    {
        Task<List<NewsDto>> GetActiveNewsAsync(string regNo);
        Task<NewsDto?> GetActivePopupNewsAsync(string regNo);
        Task DismissNewsAsync(int newsId, string regNo);
        Task<bool> HasActivePopupNewsAsync(string regNo);
    }
}