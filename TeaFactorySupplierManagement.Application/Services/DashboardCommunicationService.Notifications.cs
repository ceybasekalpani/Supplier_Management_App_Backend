using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DashboardCommunicationService
    {
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
    }
}
