using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardCommunicationService : IDashboardCommunicationService
    {
        private readonly ApplicationDbContext _context;

        public DashboardCommunicationService(ApplicationDbContext context)
        {
            _context = context;
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

            var now = DateTime.Now;
            var news = new News
            {
                Title = request.Title.Trim(),
                Content = GetNewsContent(request),
                Priority = request.Priority <= 0 ? 1 : request.Priority,
                IsActive = request.IsActive,
                ShowPopup = request.ShowPopup,
                StartDate = request.StartDate?.Date,
                EndDate = request.ExpiryDate?.Date,
                CreatedBy = CleanActor(createdBy),
                CreatedAt = now
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            return ToNewsDto(news, 0);
        }

        public async Task<DashboardNewsDto?> UpdateNewsAsync(int id, UpdateDashboardNewsDto request)
        {
            ValidateNews(request.Title, GetNewsContent(request));

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return null;
            }

            news.Title = request.Title.Trim();
            news.Content = GetNewsContent(request);
            news.Priority = request.Priority <= 0 ? 1 : request.Priority;
            news.IsActive = request.IsActive;
            news.ShowPopup = request.ShowPopup;
            news.StartDate = request.StartDate?.Date;
            news.EndDate = request.ExpiryDate?.Date;
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

            var now = DateTime.Now;
            var startDate = request.Schedule ?? request.StartDate ?? now;
            var notification = new Notification
            {
                Title = request.Title.Trim(),
                Message = request.Message.Trim(),
                Type = NormalizeNotificationType(request.Type),
                Priority = request.Priority <= 0 ? 1 : request.Priority,
                IsActive = request.IsActive,
                StartDate = startDate,
                EndDate = request.EndDate,
                TargetRegNo = request.TargetRegNo,
                CreatedBy = CleanActor(createdBy),
                CreatedAt = now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return ToNotificationDto(notification, 0, 0);
        }

        public async Task<DashboardNotificationDto?> UpdateNotificationAsync(int id, UpdateDashboardNotificationDto request)
        {
            ValidateNotification(request.Title, request.Message);

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return null;
            }

            notification.Title = request.Title.Trim();
            notification.Message = request.Message.Trim();
            notification.Type = NormalizeNotificationType(request.Type);
            notification.Priority = request.Priority <= 0 ? 1 : request.Priority;
            notification.IsActive = request.IsActive;
            notification.StartDate = request.Schedule ?? request.StartDate;
            notification.EndDate = request.EndDate;
            notification.TargetRegNo = request.TargetRegNo;
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
            return string.IsNullOrWhiteSpace(type) ? "info" : type.Trim().ToLowerInvariant();
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
    }
}
