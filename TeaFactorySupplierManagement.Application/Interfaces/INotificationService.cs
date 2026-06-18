using TeaFactorySupplierManagement.Application.DTOs.Notifications;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsForSupplierAsync(int regNo);
    Task<NotificationDto?> GetNotificationByIdAsync(int notificationId, int regNo);
    Task<int> GetUnreadCountAsync(int regNo);
    Task MarkAsReadAsync(int notificationId, int regNo);
    Task MarkAllAsReadAsync(int regNo);
    Task DismissAsync(int notificationId, int regNo);
}
}
