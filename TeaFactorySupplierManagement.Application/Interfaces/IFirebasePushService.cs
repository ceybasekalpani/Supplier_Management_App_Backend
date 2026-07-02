namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IFirebasePushService
    {
        Task SubscribeToAllSuppliersTopicAsync(string fcmToken);
        Task UnsubscribeFromAllSuppliersTopicAsync(string fcmToken);
        Task SendNewsAsync(int newsId, string title, string body, string audienceType, int? targetRegNo, string? targetRoute = null);
        Task SendNotificationAsync(int notificationId, string title, string body, string type, string audienceType, int? targetRegNo, string? targetRoute = null);
        Task SendRequestStatusChangedAsync(int regNo, string requestType, int requestId, string status, string? remarks = null, string? requestSummary = null);
        Task SendDisbursementStatusChangedAsync(int regNo, string requestType, int requestId, string status);
        Task SendConfigurationChangedAsync(string category, int id, string action);
    }
}
