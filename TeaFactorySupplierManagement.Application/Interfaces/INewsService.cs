using TeaFactorySupplierManagement.Application.DTOs.Notifications;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface INewsService
{
    Task<List<NewsDto>> GetActiveNewsAsync(int regNo);
    Task<NewsDto?> GetNewsByIdAsync(int newsId, int regNo);
    Task<NewsDto?> GetActivePopupNewsAsync(int regNo);
    Task DismissNewsAsync(int newsId, int regNo);
    Task<bool> HasActivePopupNewsAsync(int regNo);
}
}
