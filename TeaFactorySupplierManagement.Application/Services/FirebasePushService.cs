using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class FirebasePushService : IFirebasePushService
    {
        private const string AllSuppliersTopic = "all_suppliers";
        private const int FirebaseMulticastLimit = 500;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebasePushService> _logger;

        public FirebasePushService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<FirebasePushService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SubscribeToAllSuppliersTopicAsync(string fcmToken)
        {
            if (!IsEnabled() || !EnsureFirebaseApp() || string.IsNullOrWhiteSpace(fcmToken))
            {
                return;
            }

            try
            {
                await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(new[] { fcmToken.Trim() }, AllSuppliersTopic);
                _logger.LogInformation("Subscribed FCM token to topic {Topic}.", AllSuppliersTopic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not subscribe FCM token to topic {Topic}.", AllSuppliersTopic);
            }
        }

        public async Task UnsubscribeFromAllSuppliersTopicAsync(string fcmToken)
        {
            if (!IsEnabled() || !EnsureFirebaseApp() || string.IsNullOrWhiteSpace(fcmToken))
            {
                return;
            }

            try
            {
                await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(new[] { fcmToken.Trim() }, AllSuppliersTopic);
                _logger.LogInformation("Unsubscribed FCM token from topic {Topic}.", AllSuppliersTopic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not unsubscribe FCM token from topic {Topic}.", AllSuppliersTopic);
            }
        }

        public Task SendNewsAsync(int newsId, string title, string body, string audienceType, int? targetRegNo, string? targetRoute = null)
        {
            return SendAsync(
                title,
                body,
                NormalizeAudienceType(audienceType),
                targetRegNo,
                CleanOptional(targetRoute),
                new Dictionary<string, string>
                {
                    ["type"] = "news",
                    ["newsId"] = newsId.ToString()
                });
        }

        public Task SendNotificationAsync(int notificationId, string title, string body, string type, string audienceType, int? targetRegNo, string? targetRoute = null)
        {
            return SendAsync(
                title,
                body,
                NormalizeAudienceType(audienceType),
                targetRegNo,
                CleanOptional(targetRoute),
                new Dictionary<string, string>
                {
                    ["type"] = "notification",
                    ["notificationId"] = notificationId.ToString(),
                    ["notificationType"] = string.IsNullOrWhiteSpace(type) ? "general" : type.Trim()
                });
        }

        public Task SendRequestStatusChangedAsync(int regNo, string requestType, int requestId, string status, string? remarks = null, string? requestSummary = null)
        {
            var normalizedType = NormalizeRequestType(requestType);
            var normalizedStatus = NormalizeStatus(status);
            var title = BuildRequestStatusTitle(normalizedType, normalizedStatus);
            var body = BuildRequestStatusBody(normalizedType, normalizedStatus, requestSummary, remarks);

            return SendAsync(
                title,
                body,
                "SpecificSupplier",
                regNo,
                null,
                new Dictionary<string, string>
                {
                    ["type"] = "request_status_changed",
                    ["requestType"] = normalizedType,
                    ["requestId"] = requestId.ToString(),
                    ["status"] = normalizedStatus
                });
        }

        public Task SendDisbursementStatusChangedAsync(int regNo, string requestType, int requestId, string status)
        {
            var normalizedType = NormalizeRequestType(requestType);
            var normalizedStatus = NormalizeStatus(status);
            var title = BuildDisbursementStatusTitle(normalizedType, normalizedStatus);
            var body = BuildDisbursementStatusBody(normalizedType, normalizedStatus);

            return SendAsync(
                title,
                body,
                "SpecificSupplier",
                regNo,
                null,
                new Dictionary<string, string>
                {
                    ["type"] = "disbursement_status_changed",
                    ["requestType"] = normalizedType,
                    ["requestId"] = requestId.ToString(),
                    ["status"] = normalizedStatus
                });
        }

        public Task SendConfigurationChangedAsync(string category, int id, string action)
        {
            var normalizedCategory = NormalizeConfigurationCategory(category);
            var normalizedAction = NormalizeStatus(action);

            return SendAsync(
                "App settings updated",
                $"{ToDisplayConfigurationCategory(normalizedCategory)} list has been updated.",
                "AllSuppliers",
                null,
                null,
                new Dictionary<string, string>
                {
                    ["type"] = "configuration_changed",
                    ["category"] = normalizedCategory,
                    ["id"] = id.ToString(),
                    ["action"] = normalizedAction
                });
        }
    }
}
