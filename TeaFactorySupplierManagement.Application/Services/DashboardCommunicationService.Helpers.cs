using System.Text.Json;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DashboardCommunicationService
    {
        private static DashboardNewsDto ToNewsDto(News news, int dismissedCount)
        {
            var status = GetNewsStatus(news);
            var endDate = news.EndDate?.ToString("yyyy-MM-dd");

            return new DashboardNewsDto
            {
                Id = news.Id,
                Title = news.Title,
                Description = news.Content,
                Content = news.Content,
                Created = news.CreatedAt.ToString("yyyy-MM-dd"),
                Expiry = endDate ?? "No expiry",
                Status = status,
                Priority = news.Priority,
                IsActive = news.IsActive,
                ShowPopup = news.ShowPopup,
                StartDate = news.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate,
                AudienceType = GetAudienceType(news.TargetRegNo, news.TargetRoute),
                TargetRegNo = news.TargetRegNo,
                TargetRoute = news.TargetRoute,
                CreatedBy = news.CreatedBy,
                DismissedCount = dismissedCount
            };
        }

        private static DashboardNotificationDto ToNotificationDto(Notification notification, int readCount, int dismissedCount)
        {
            var status = GetNotificationStatus(notification);
            var startDate = notification.StartDate?.ToString("yyyy-MM-ddTHH:mm");

            return new DashboardNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Status = status,
                ScheduledFor = status == "scheduled" ? startDate : null,
                SentAt = status == "delivered" ? notification.CreatedAt.ToString("yyyy-MM-ddTHH:mm") : null,
                Priority = notification.Priority,
                IsActive = notification.IsActive,
                StartDate = startDate,
                EndDate = notification.EndDate?.ToString("yyyy-MM-ddTHH:mm"),
                TargetRegNo = notification.TargetRegNo,
                TargetRoute = notification.TargetRoute,
                AudienceType = GetAudienceType(notification.TargetRegNo, notification.TargetRoute),
                CreatedBy = notification.CreatedBy,
                ReadCount = readCount,
                DismissedCount = dismissedCount
            };
        }

        private static string GetNewsStatus(News news)
        {
            var now = DateTime.Now.Date;

            if (news.EndDate.HasValue && news.EndDate.Value.Date < now)
            {
                return "expired";
            }

            if (!news.IsActive || (news.StartDate.HasValue && news.StartDate.Value.Date > now))
            {
                return "draft";
            }

            return "active";
        }

        private static string GetNotificationStatus(Notification notification)
        {
            var now = DateTime.Now;

            if (!notification.IsActive)
            {
                return "failed";
            }

            if (notification.StartDate.HasValue && notification.StartDate.Value > now)
            {
                return "scheduled";
            }

            if (notification.EndDate.HasValue && notification.EndDate.Value < now)
            {
                return "failed";
            }

            return "delivered";
        }

        private static string NormalizeFilter(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.Trim().ToLowerInvariant();
        }

        private static string NormalizeNotificationType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "general";
            }

            return type.Trim().ToLowerInvariant();
        }

        private static string NormalizeAudienceType(string? audienceType)
        {
            return string.Equals(audienceType, "SpecificSupplier", StringComparison.OrdinalIgnoreCase)
                ? "SpecificSupplier"
                : string.Equals(audienceType, "Route", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(audienceType, "RouteWise", StringComparison.OrdinalIgnoreCase)
                    ? "Route"
                    : "AllSuppliers";
        }

        private static int? NormalizeTargetRegNo(string? audienceType, object? targetRegNo)
        {
            if (NormalizeAudienceType(audienceType) != "SpecificSupplier")
            {
                return null;
            }

            return ParseRegNo(targetRegNo);
        }

        private static string? NormalizeTargetRoute(string? audienceType, string? targetRoute)
        {
            return NormalizeAudienceType(audienceType) == "Route"
                ? CleanOptional(targetRoute)
                : null;
        }

        private static bool ShouldPublishNow(bool publishNow, bool isActive, DateTime? startDate, DateTime? endDate)
        {
            var now = DateTime.Now;
            return publishNow &&
                   isActive &&
                   (!startDate.HasValue || startDate.Value <= now) &&
                   (!endDate.HasValue || endDate.Value >= now);
        }

        private static string CleanActor(string? actor)
        {
            return string.IsNullOrWhiteSpace(actor) ? "Dashboard Admin" : actor.Trim();
        }

        private static string GetNewsContent(CreateDashboardNewsDto request)
        {
            var content = string.IsNullOrWhiteSpace(request.Content) ? request.Description : request.Content;
            return content.Trim();
        }

        private static void ValidateNews(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("News title and content are required.");
            }
        }

        private static void ValidateNotification(string title, string message)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Notification title and message are required.");
            }
        }

        private static void ValidateAudience(string? audienceType, object? targetRegNo, string? targetRoute)
        {
            if (NormalizeAudienceType(audienceType) == "SpecificSupplier" && !ParseRegNo(targetRegNo).HasValue)
            {
                throw new ArgumentException("Target supplier registration number is required for specific supplier audience.");
            }

            if (NormalizeAudienceType(audienceType) == "Route" && string.IsNullOrWhiteSpace(targetRoute))
            {
                throw new ArgumentException("Target route is required for route audience.");
            }
        }

        private static int? ParseRegNo(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue > 0 ? intValue : null;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out var numberValue))
                {
                    return numberValue > 0 ? numberValue : null;
                }

                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return int.TryParse(jsonElement.GetString(), out var stringNumber) && stringNumber > 0
                        ? stringNumber
                        : null;
                }

                return null;
            }

            return int.TryParse(Convert.ToString(value), out var parsed) && parsed > 0 ? parsed : null;
        }

        private static string GetAudienceType(int? targetRegNo, string? targetRoute)
        {
            if (targetRegNo.HasValue)
            {
                return "SpecificSupplier";
            }

            return string.IsNullOrWhiteSpace(targetRoute) ? "AllSuppliers" : "Route";
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void EnsureCanDelete(DateTime createdAt, string itemName)
        {
            if (createdAt < DateTime.Now.AddHours(-24))
            {
                throw new InvalidOperationException($"{itemName} can only be deleted within 24 hours after creation.");
            }
        }
    }
}
