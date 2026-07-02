using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class FirebasePushService : IFirebasePushService
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

        private async Task SendAsync(
            string title,
            string body,
            string audienceType,
            int? targetRegNo,
            string? targetRoute,
            Dictionary<string, string> data)
        {
            if (!IsEnabled() || !EnsureFirebaseApp())
            {
                _logger.LogWarning("Firebase push skipped. Enabled={Enabled}, ServiceAccountPath={ServiceAccountPath}.",
                    IsEnabled(),
                    _configuration["Firebase:ServiceAccountPath"]);
                return;
            }

            if (audienceType == "SpecificSupplier" && targetRegNo.HasValue)
            {
                var tokens = await _context.MobileDeviceTokens
                    .AsNoTracking()
                    .Where(x => x.RegNo == targetRegNo.Value && x.IsActive)
                    .Select(x => x.FcmToken)
                    .Distinct()
                    .ToListAsync();

                if (tokens.Count == 0)
                {
                    _logger.LogWarning("Firebase push skipped. No active FCM tokens found for supplier {RegNo}.", targetRegNo.Value);
                    return;
                }

                await SendToTokensAsync(tokens, title, body, data);
                return;
            }

            if (audienceType == "Route" && !string.IsNullOrWhiteSpace(targetRoute))
            {
                var routeTokens = await _context.MobileDeviceTokens
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.Supplier != null && x.Supplier.Route == targetRoute)
                    .Select(x => x.FcmToken)
                    .Distinct()
                    .ToListAsync();

                if (routeTokens.Count == 0)
                {
                    _logger.LogWarning("Firebase push skipped. No active FCM tokens found for route {Route}.", targetRoute);
                    return;
                }

                data["targetRoute"] = targetRoute;
                await SendToTokensAsync(routeTokens, title, body, data);
                return;
            }

            var allTokens = await _context.MobileDeviceTokens
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.FcmToken)
                .Distinct()
                .ToListAsync();

            if (allTokens.Count == 0)
            {
                _logger.LogWarning("Firebase push skipped. No active FCM tokens found for all suppliers.");
                return;
            }

            await SendToTokensAsync(allTokens, title, body, data);
        }

        private async Task SendToTokensAsync(List<string> tokens, string title, string body, Dictionary<string, string> data)
        {
            var totalSuccess = 0;
            var totalFailure = 0;

            foreach (var batch in tokens.Chunk(FirebaseMulticastLimit))
            {
                var batchTokens = batch.ToList();

                try
                {
                    var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
                    {
                        Tokens = batchTokens,
                        Notification = BuildNotification(title, body),
                        Android = BuildAndroidConfig(),
                        Apns = BuildApnsConfig(title, body),
                        Data = data
                    });

                    totalSuccess += response.SuccessCount;
                    totalFailure += response.FailureCount;
                    await DeactivateInvalidTokensAsync(batchTokens, response);
                }
                catch (Exception ex)
                {
                    totalFailure += batchTokens.Count;
                    _logger.LogError(ex, "Firebase multicast send failed for {TokenCount} token(s). DataType={Type}.",
                        batchTokens.Count,
                        data.GetValueOrDefault("type"));
                }
            }

            _logger.LogInformation("Firebase push completed. Type={Type}, Success={SuccessCount}, Failure={FailureCount}.",
                data.GetValueOrDefault("type"),
                totalSuccess,
                totalFailure);
        }

        private static FirebaseAdmin.Messaging.Notification BuildNotification(string title, string body)
        {
            return new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            };
        }

        private static AndroidConfig BuildAndroidConfig()
        {
            return new AndroidConfig
            {
                Priority = Priority.High,
                CollapseKey = "supplier_app_notification",
                Notification = new AndroidNotification
                {
                    ChannelId = "default",
                    Sound = "default",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            };
        }

        private static ApnsConfig BuildApnsConfig(string title, string body)
        {
            return new ApnsConfig
            {
                Aps = new Aps
                {
                    Alert = new ApsAlert
                    {
                        Title = title,
                        Body = body
                    },
                    Sound = "default"
                }
            };
        }

        private bool IsEnabled()
        {
            return bool.TryParse(_configuration["Firebase:Enabled"], out var enabled) && enabled;
        }

        private bool EnsureFirebaseApp()
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                return true;
            }

            var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];
            serviceAccountPath = ResolveServiceAccountPath(serviceAccountPath);
            if (string.IsNullOrWhiteSpace(serviceAccountPath))
            {
                _logger.LogWarning("Firebase service account file was not found. Configured path: {ConfiguredPath}.",
                    _configuration["Firebase:ServiceAccountPath"]);
                return false;
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(serviceAccountPath)
            });

            _logger.LogInformation("Firebase Admin SDK initialized with service account path {ServiceAccountPath}.", serviceAccountPath);

            return true;
        }

        private static string? ResolveServiceAccountPath(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            var cleanPath = configuredPath.Trim();
            if (Path.IsPathRooted(cleanPath))
            {
                return File.Exists(cleanPath) ? cleanPath : null;
            }

            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), cleanPath),
                Path.Combine(AppContext.BaseDirectory, cleanPath),
                Path.Combine(Directory.GetCurrentDirectory(), "TeaFactorySupplierManagement.API", cleanPath)
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private async Task DeactivateInvalidTokensAsync(IReadOnlyList<string> tokens, BatchResponse response)
        {
            var invalidTokens = new List<string>();
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var sendResponse = response.Responses[i];
                if (sendResponse.IsSuccess)
                {
                    continue;
                }

                _logger.LogWarning(
                    sendResponse.Exception,
                    "Firebase send failed for token index {TokenIndex}. ErrorCode={ErrorCode}.",
                    i,
                    sendResponse.Exception?.MessagingErrorCode);

                if (sendResponse.Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                {
                    invalidTokens.Add(tokens[i]);
                }
            }

            if (invalidTokens.Count == 0)
            {
                return;
            }

            _logger.LogWarning("Deactivating {InvalidTokenCount} invalid Firebase token(s).", invalidTokens.Count);

            var rows = await _context.MobileDeviceTokens
                .Where(x => invalidTokens.Contains(x.FcmToken))
                .ToListAsync();

            foreach (var row in rows)
            {
                row.IsActive = false;
                row.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static string NormalizeAudienceType(string? value)
        {
            if (string.Equals(value, "SpecificSupplier", StringComparison.OrdinalIgnoreCase))
            {
                return "SpecificSupplier";
            }

            return string.Equals(value, "Route", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "RouteWise", StringComparison.OrdinalIgnoreCase)
                ? "Route"
                : "AllSuppliers";
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeRequestType(string? value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "cash" or "advance" => "advance",
                "fertilizer" => "fertilizer",
                "item" or "items" => "items",
                _ => "advance"
            };
        }

        private static string NormalizeStatus(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "updated"
                : value.Trim().ToLowerInvariant();
        }

        private static string NormalizeConfigurationCategory(string? value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "fertilizer" or "fertilizers" => "fertilizer",
                "item" or "items" => "item",
                _ => "configuration"
            };
        }

        private static string BuildRequestStatusTitle(string requestType, string status)
        {
            return $"{ToDisplayRequestType(requestType)} request {ToDisplayStatus(status)}";
        }

        private static string BuildRequestStatusBody(string requestType, string status, string? requestSummary, string? remarks)
        {
            var displayType = ToDisplayRequestType(requestType).ToLowerInvariant();
            var cleanSummary = string.IsNullOrWhiteSpace(requestSummary) ? string.Empty : $" ({requestSummary.Trim()})";
            var message = $"Your {displayType} request{cleanSummary} has been {ToDisplayStatus(status).ToLowerInvariant()}.";

            if (status == "rejected" && !string.IsNullOrWhiteSpace(remarks))
            {
                message += $" Reason: {remarks.Trim()}";
            }

            return message;
        }

        private static string BuildDisbursementStatusTitle(string requestType, string status)
        {
            return $"{ToDisplayRequestType(requestType)} disbursement {ToDisplayStatus(status)}";
        }

        private static string BuildDisbursementStatusBody(string requestType, string status)
        {
            return $"Your {ToDisplayRequestType(requestType).ToLowerInvariant()} disbursement has been {ToDisplayStatus(status).ToLowerInvariant()}.";
        }

        private static string ToDisplayRequestType(string requestType)
        {
            return requestType switch
            {
                "advance" => "Advance",
                "fertilizer" => "Fertilizer",
                "items" => "Item",
                _ => "Request"
            };
        }

        private static string ToDisplayStatus(string status)
        {
            return status switch
            {
                "approved" => "Approved",
                "rejected" => "Rejected",
                "issued" => "Issued",
                "paid" => "Paid",
                _ => "Updated"
            };
        }

        private static string ToDisplayConfigurationCategory(string category)
        {
            return category switch
            {
                "fertilizer" => "Fertilizer type",
                "item" => "Item type",
                _ => "Configuration"
            };
        }
    }
}
