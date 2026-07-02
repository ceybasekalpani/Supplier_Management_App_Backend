using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardCommunicationService : IDashboardCommunicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;

        public DashboardCommunicationService(ApplicationDbContext context, IFirebasePushService firebasePushService)
        {
            _context = context;
            _firebasePushService = firebasePushService;
        }

        public async Task<CommunicationDashboardDto> GetAllAsync()
        {
            return new CommunicationDashboardDto
            {
                News = await GetNewsAsync(null),
                Notifications = await GetNotificationsAsync(null)
            };
        }

        public async Task<List<DashboardNewsDto>> GetNewsAsync(string? status)
        {
            var news = await _context.News
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var dismissedCounts = await _context.NewsDismissed
                .AsNoTracking()
                .GroupBy(x => x.NewsId)
                .Select(x => new { NewsId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.NewsId, x => x.Count);

            var rows = news
                .Select(x => ToNewsDto(x, dismissedCounts.TryGetValue(x.Id, out var count) ? count : 0))
                .ToList();

            var normalizedStatus = NormalizeFilter(status);
            if (normalizedStatus == "all")
            {
                return rows;
            }

            return rows.Where(x => x.Status == normalizedStatus).ToList();
        }

        public async Task<DashboardNewsDto> CreateNewsAsync(CreateDashboardNewsDto request, string? createdBy)
        {
            ValidateNews(request.Title, GetNewsContent(request));
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var now = DateTime.Now;
            var news = new News
            {
                Title = request.Title.Trim(),
                Content = GetNewsContent(request),
                Priority = request.Priority <= 0 ? 1 : request.Priority,
                IsActive = isActive,
                ShowPopup = request.ShowPopup,
                StartDate = request.StartDate?.Date,
                EndDate = request.ExpiryDate?.Date,
                TargetRegNo = targetRegNo,
                TargetRoute = targetRoute,
                CreatedBy = CleanActor(createdBy),
                CreatedAt = now
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            if (ShouldPublishNow(request.PublishNow, news.IsActive, news.StartDate, news.EndDate))
            {
                await _firebasePushService.SendNewsAsync(
                    news.Id,
                    news.Title,
                    news.Content,
                    NormalizeAudienceType(request.AudienceType),
                    news.TargetRegNo,
                    news.TargetRoute);
            }

            return ToNewsDto(news, 0);
        }

        public async Task<DashboardNewsDto?> UpdateNewsAsync(int id, UpdateDashboardNewsDto request)
        {
            ValidateNews(request.Title, GetNewsContent(request));
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return null;
            }

            news.Title = request.Title.Trim();
            news.Content = GetNewsContent(request);
            news.Priority = request.Priority <= 0 ? 1 : request.Priority;
            news.IsActive = isActive;
            news.ShowPopup = request.ShowPopup;
            news.StartDate = request.StartDate?.Date;
            news.EndDate = request.ExpiryDate?.Date;
            news.TargetRegNo = targetRegNo;
            news.TargetRoute = targetRoute;
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var dismissedCount = await _context.NewsDismissed.CountAsync(x => x.NewsId == id);
            return ToNewsDto(news, dismissedCount);
        }

        public async Task<bool> DeleteNewsAsync(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return false;
            }

            EnsureCanDelete(news.CreatedAt, "News");

            var dismissedRows = await _context.NewsDismissed
                .Where(x => x.NewsId == id)
                .ToListAsync();

            _context.NewsDismissed.RemoveRange(dismissedRows);
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DashboardNotificationDto>> GetNotificationsAsync(string? status)
        {
            var notifications = await _context.Notifications
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var readCounts = await _context.NotificationRead
                .AsNoTracking()
                .GroupBy(x => x.NotificationId)
                .Select(x => new { NotificationId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.NotificationId, x => x.Count);

            var dismissedCounts = await _context.NotificationDismissed
                .AsNoTracking()
                .GroupBy(x => x.NotificationId)
                .Select(x => new { NotificationId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.NotificationId, x => x.Count);

            var rows = notifications
                .Select(x => ToNotificationDto(
                    x,
                    readCounts.TryGetValue(x.Id, out var readCount) ? readCount : 0,
                    dismissedCounts.TryGetValue(x.Id, out var dismissedCount) ? dismissedCount : 0))
                .ToList();

            var normalizedStatus = NormalizeFilter(status);
            if (normalizedStatus == "all")
            {
                return rows;
            }

            return rows.Where(x => x.Status == normalizedStatus).ToList();
        }

        public async Task<DashboardNotificationDto> CreateNotificationAsync(CreateDashboardNotificationDto request, string? createdBy)
        {
            ValidateNotification(request.Title, request.Message);
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var now = DateTime.Now;
            var startDate = request.Schedule ?? request.StartDate ?? now;
            var notification = new Notification
            {
                Title = request.Title.Trim(),
                Message = request.Message.Trim(),
                Type = NormalizeNotificationType(request.Type),
                Priority = request.Priority <= 0 ? 1 : request.Priority,
                IsActive = isActive,
                StartDate = startDate,
                EndDate = request.EndDate,
                TargetRegNo = targetRegNo,
                TargetRoute = targetRoute,
                CreatedBy = CleanActor(createdBy),
                CreatedAt = now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            if (ShouldPublishNow(request.PublishNow, notification.IsActive, notification.StartDate, notification.EndDate))
            {
                await _firebasePushService.SendNotificationAsync(
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    notification.Type,
                    NormalizeAudienceType(request.AudienceType),
                    notification.TargetRegNo,
                    notification.TargetRoute);
            }

            return ToNotificationDto(notification, 0, 0);
        }

        public async Task<DashboardNotificationDto?> UpdateNotificationAsync(int id, UpdateDashboardNotificationDto request)
        {
            ValidateNotification(request.Title, request.Message);
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return null;
            }

            notification.Title = request.Title.Trim();
            notification.Message = request.Message.Trim();
            notification.Type = NormalizeNotificationType(request.Type);
            notification.Priority = request.Priority <= 0 ? 1 : request.Priority;
            notification.IsActive = isActive;
            notification.StartDate = request.Schedule ?? request.StartDate;
            notification.EndDate = request.EndDate;
            notification.TargetRegNo = targetRegNo;
            notification.TargetRoute = targetRoute;
            notification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var readCount = await _context.NotificationRead.CountAsync(x => x.NotificationId == id);
            var dismissedCount = await _context.NotificationDismissed.CountAsync(x => x.NotificationId == id);
            return ToNotificationDto(notification, readCount, dismissedCount);
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return false;
            }

            EnsureCanDelete(notification.CreatedAt, "Notification");

            var readRows = await _context.NotificationRead
                .Where(x => x.NotificationId == id)
                .ToListAsync();
            var dismissedRows = await _context.NotificationDismissed
                .Where(x => x.NotificationId == id)
                .ToListAsync();

            _context.NotificationRead.RemoveRange(readRows);
            _context.NotificationDismissed.RemoveRange(dismissedRows);
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

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
