using TeaFactorySupplierManagement.Application.DTOs.Notifications;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsForSupplierAsync(string regNo);
        Task<int> GetUnreadCountAsync(string regNo);
        Task MarkAsReadAsync(int notificationId, string regNo);
        Task MarkAllAsReadAsync(string regNo);
    }
}