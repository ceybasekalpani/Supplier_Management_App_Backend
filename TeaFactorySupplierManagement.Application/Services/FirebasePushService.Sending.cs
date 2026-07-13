using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class FirebasePushService
    {
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
    }
}
